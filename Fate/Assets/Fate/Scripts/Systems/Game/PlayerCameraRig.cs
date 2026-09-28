using System.Linq;
using Cinemachine;
using FishNet.Object;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Scopes the Online FPS/TPS Kit's camera + input rig to whoever actually controls this
    /// player instance.
    ///
    /// The kit was authored assuming exactly one player exists in the scene: every instance
    /// carries its own MainCamera-tagged Camera + AudioListener + CinemachineBrain, and its
    /// virtual cameras compete for "Live" status globally across every CinemachineBrain in the
    /// scene - Cinemachine has no built-in concept of "this vcam belongs to that player". With
    /// more than one instance active, every brain ends up locking onto whichever vcam wins that
    /// global priority race, so multiple cameras render the exact same view.
    ///
    /// Fix:
    /// - Non-owned instances (remote network players) are pure network-driven avatars: their
    ///   entire camera object (Camera + AudioListener + CinemachineBrain) and their local input
    ///   scripts are disabled once, at spawn. Zero ongoing per-frame cost.
    /// - Owned instances (this client's local players - up to <see cref="MaxLocalSlots"/> for
    ///   split-screen) are assigned a slot. Each slot has a reserved, Cinemachine-only layer
    ///   (VCamSlot0..VCamSlot3 - see Project Settings > Tags and Layers) that nothing else
    ///   renders or collides with. Moving this instance's virtual cameras onto its slot's layer
    ///   and restricting its own Camera's culling mask to that layer (among the reserved ones)
    ///   means CinemachineBrain.TopCameraFromPriorityQueue() - which filters candidate vcams by
    ///   Camera.cullingMask vs. the vcam GameObject's layer - can only ever see this instance's
    ///   own vcams. Slot 0 also keeps the only enabled AudioListener; slots 1-3 stay muted to
    ///   avoid multiple simultaneous listeners.
    ///
    /// Slot assignment is a temporary manual/inspector value for now (<see cref="localSlotIndex"/>
    /// / <see cref="totalLocalPlayers"/>). A later pass wires this up to the local split-screen
    /// join lobby (device pairing, quadrant picker) and the per-connection multi-object network
    /// spawn, which will call <see cref="ApplyLocalSlot"/> directly instead of relying on the
    /// inspector defaults.
    ///
    /// NOTE: originally this only scoped the camera and camera-input scripts (Input_Handler,
    /// CameraController, CameraSwitcher) that were causing the reported cross-talk bug, leaving
    /// weapon and movement scripts untouched. That boundary has since widened deliberately:
    /// CharacterMove (and its move states) now read from this instance's own PlayerInputRig
    /// instead of the global Input.* state, so it needs the same owner-only gating and is included
    /// in <see cref="_ownerOnlyBehaviours"/> below alongside PlayerInputRig itself.
    /// </summary>
    public class PlayerCameraRig : NetworkBehaviour
    {
        public const int MaxLocalSlots = 4;

        private static readonly string[] VCamLayerNames =
        {
            "VCamSlot0", "VCamSlot1", "VCamSlot2", "VCamSlot3"
        };

        [Header("Local split-screen slot (temporary - will be driven by the join lobby later)")]
        [SerializeField, Range(0, MaxLocalSlots - 1)]
        private int localSlotIndex;

        [SerializeField, Range(1, MaxLocalSlots)]
        private int totalLocalPlayers = 1;

        private Camera _playerCamera;
        private AudioListener _audioListener;
        private CinemachineVirtualCamera[] _virtualCameras;
        private Behaviour[] _ownerOnlyBehaviours;
        private PlayerInputRig _inputRig;

        private void Awake()
        {
            _playerCamera = GetComponentInChildren<Camera>(true);
            _audioListener = GetComponentInChildren<AudioListener>(true);
            _virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>(true);
            _inputRig = GetComponentInChildren<PlayerInputRig>(true);

            // CharacterMove (and therefore every move state) is included here too: those scripts
            // are being migrated (see Assets/Fate/Docs/SplitScreenNetworkedPlayers.md) to read
            // from this instance's own PlayerInputRig instead of the global Input.* state, so a
            // non-owned instance - which never gets an initialized rig - must stop ticking
            // movement locally the same way it already stops reading camera/weapon input below.
            // Network state already drives a remote instance's visible position/animation.
            _ownerOnlyBehaviours = new Behaviour[]
            {
                GetComponentInChildren<Input_Handler>(true),
                GetComponentInChildren<CameraController>(true),
                GetComponentInChildren<CameraSwitcher>(true),
                GetComponentInChildren<CharacterMove>(true),
                _inputRig,
            }.Where(b => b != null).ToArray();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                DisableRemoteRig();
                return;
            }

            ApplyLocalSlot(localSlotIndex, totalLocalPlayers);
        }

        /// <summary>
        /// Remote (non-owned) players don't need a camera, an audio listener, or any of the
        /// local-input-reading scripts at all - they're driven entirely by network state.
        /// </summary>
        private void DisableRemoteRig()
        {
            if (_playerCamera != null)
                _playerCamera.gameObject.SetActive(false);

            foreach (Behaviour behaviour in _ownerOnlyBehaviours)
                behaviour.enabled = false;
        }

        /// <summary>
        /// Assigns this owned instance to a local split-screen slot: puts its virtual cameras on
        /// that slot's reserved layer, restricts its own camera's culling mask so its
        /// CinemachineBrain only ever considers its own vcams, sets its viewport rect for the
        /// given local player count, and mutes every AudioListener except slot 0's.
        /// </summary>
        public void ApplyLocalSlot(int slotIndex, int playerCount)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, MaxLocalSlots - 1);
            playerCount = Mathf.Clamp(playerCount, 1, MaxLocalSlots);
            localSlotIndex = slotIndex;
            totalLocalPlayers = playerCount;

            int vcamLayer = LayerMask.NameToLayer(VCamLayerNames[slotIndex]);
            if (vcamLayer < 0)
            {
                Debug.LogError($"{nameof(PlayerCameraRig)}: layer '{VCamLayerNames[slotIndex]}' is not defined in " +
                                "Project Settings > Tags and Layers. Add it before assigning local slots.", this);
                return;
            }

            foreach (CinemachineVirtualCamera vcam in _virtualCameras)
                vcam.gameObject.layer = vcamLayer;

            if (_playerCamera != null)
            {
                int reservedMask = 0;
                foreach (string layerName in VCamLayerNames)
                {
                    int layer = LayerMask.NameToLayer(layerName);
                    if (layer >= 0)
                        reservedMask |= 1 << layer;
                }

                // Keep every normal rendering layer, but among the reserved vcam-only layers
                // only let this slot's own layer through - this is what stops
                // CinemachineBrain.TopCameraFromPriorityQueue() from ever picking up another
                // local player's (or a remote player's) virtual cameras.
                int mask = _playerCamera.cullingMask & ~reservedMask;
                mask |= 1 << vcamLayer;
                _playerCamera.cullingMask = mask;

                _playerCamera.rect = GetViewportRect(slotIndex, playerCount);
            }

            if (_audioListener != null)
                _audioListener.enabled = slotIndex == 0;

            if (_inputRig != null)
            {
                _inputRig.Initialize(slotIndex);

                var coordinator = FindObjectOfType<LocalInputCoordinator>();
                if (coordinator != null)
                    coordinator.Register(_inputRig);
                else
                    Debug.LogWarning($"{nameof(PlayerCameraRig)}: no {nameof(LocalInputCoordinator)} found in the " +
                                      "scene - this slot's Pause action will do nothing.", this);
            }
        }

        private static Rect GetViewportRect(int slotIndex, int playerCount)
        {
            switch (playerCount)
            {
                case 1:
                    return new Rect(0f, 0f, 1f, 1f);
                case 2:
                    return slotIndex == 0
                        ? new Rect(0f, 0f, 0.5f, 1f)
                        : new Rect(0.5f, 0f, 0.5f, 1f);
                case 3:
                    switch (slotIndex)
                    {
                        case 0: return new Rect(0f, 0.5f, 0.5f, 0.5f);
                        case 1: return new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                        default: return new Rect(0f, 0f, 1f, 0.5f);
                    }
                default: // 4
                    switch (slotIndex)
                    {
                        case 0: return new Rect(0f, 0.5f, 0.5f, 0.5f);
                        case 1: return new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                        case 2: return new Rect(0f, 0f, 0.5f, 0.5f);
                        default: return new Rect(0.5f, 0f, 0.5f, 0.5f);
                    }
            }
        }
    }
}
