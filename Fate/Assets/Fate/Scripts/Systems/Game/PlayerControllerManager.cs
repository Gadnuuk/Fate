using System;
using System.Collections.Generic;
using Fate.Systems.DeveloperMenu;
using Fate.Systems.InputActions;
using Fate.Systems.Player;
using Fate.Systems.Utilities;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Central hub for <see cref="PlayerController"/> lifecycle. Owns a single persistent "global"
    /// controller that listens to every device (see <see cref="GlobalController"/>) and is never
    /// despawned, plus whichever per-player controllers are currently configured via
    /// <see cref="SpawnController"/>/<see cref="ApplyConfiguration"/>.
    ///
    /// Persistent singleton (see <see cref="PersistentSingleton{T}"/>), same pattern as
    /// <see cref="ProfileManager"/> - controllers spawned here survive scene loads.
    ///
    /// Also the single source of truth for the active render camera(s), in menus and gameplay
    /// alike - see <see cref="mainCamera"/>, <see cref="GetCameraFor"/>, <see cref="EnableSplitScreen"/>.
    /// </summary>
    public class PlayerControllerManager : PersistentSingleton<PlayerControllerManager>
    {
        [SerializeField]
        private PlayerController playerControllerPrefab;

        [Tooltip("Assigned to the global controller's ActiveContext right after it's spawned. Per-player controllers keep whatever ActiveContext is already configured on playerControllerPrefab.")]
        [SerializeField]
        private InputContext globalControllerContext;

        [Tooltip("Switched to on a per-player controller's ActiveContext by PawnNetworkBridge once it possesses that controller with a spawned networked pawn - see PawnNetworkBridge.OnStartClient.")]
        [SerializeField]
        private InputContext gameplayContext;

        [Tooltip("The single render camera used whenever split-screen is disabled. Lives on this persistent singleton so it survives every scene load instead of each scene owning its own.")]
        [SerializeField]
        private Camera mainCamera;

        [Tooltip("Authored viewport rects per player count. Swap at runtime via SetLayoutSet to change split-screen layout.")]
        [SerializeField]
        private SplitScreenLayoutSet layoutSet;

        private PlayerController _globalController;
        private readonly List<PlayerController> _activeControllers = new();
        private readonly Dictionary<PlayerId, PlayerController> _controllersByPlayerId = new();
        private readonly Dictionary<PlayerController, Camera> _splitScreenCameras = new();
        private bool _isSplitScreenEnabled;

        /// <summary>The single controller that listens to every device regardless of player, used to drive every <see cref="Pawn.IsGlobal"/> pawn. Spawned on first access.</summary>
        public PlayerController GlobalController
        {
            get
            {
                if (_globalController == null)
                {
                    _globalController = SpawnControllerInternal(ownerPlayerId: null, targetDevice: null);
                    _globalController.ActiveContext = globalControllerContext;
                }

                return _globalController;
            }
        }

        /// <summary>Every currently spawned per-player controller (excludes <see cref="GlobalController"/>).</summary>
        public IReadOnlyList<PlayerController> ActiveControllers => _activeControllers;

        /// <summary>The <see cref="InputContext"/> a per-player controller should switch to once it possesses a spawned networked pawn - see <see cref="PawnNetworkBridge.OnStartClient"/>.</summary>
        public InputContext GameplayContext => gameplayContext;

        /// <summary>Spawns a <see cref="PlayerController"/> bound to <paramref name="playerId"/> and <paramref name="device"/>.</summary>
        public PlayerController SpawnController(PlayerId playerId, InputDevice device)
        {
            PlayerController controller = SpawnControllerInternal(playerId, device);
            _activeControllers.Add(controller);
            _controllersByPlayerId[playerId] = controller;
            return controller;
        }

        /// <summary>
        /// Looks up the currently active per-player controller owned by <paramref name="playerId"/>, if any.
        /// Used by networked pawn spawn-bridging code (e.g. a pawn's owning client resolving which local
        /// controller should possess it) to stay decoupled from how/when that controller was created.
        /// </summary>
        public bool TryGetController(PlayerId playerId, out PlayerController controller) =>
            _controllersByPlayerId.TryGetValue(playerId, out controller);

        /// <summary>
        /// Despawns every current per-player controller and spawns a fresh one per assignment.
        /// The global controller is never touched.
        /// </summary>
        // TODO: once pawn possession bridges to networked characters, this needs FishNet
        // ownership/RPC handling - today everything here is local-client-only.
        public void ApplyConfiguration(IReadOnlyList<PlayerControllerAssignment> assignments)
        {
            DespawnActiveControllers();

            foreach (PlayerControllerAssignment assignment in assignments)
                SpawnController(assignment.PlayerId, assignment.Device);
        }

        /// <summary>Destroys every current per-player controller. The global controller is never touched.</summary>
        public void DespawnActiveControllers()
        {
            foreach (PlayerController controller in _activeControllers)
            {
                if (controller != null)
                    Destroy(controller.gameObject);
            }

            _activeControllers.Clear();
            _controllersByPlayerId.Clear();
        }

        private PlayerController SpawnControllerInternal(PlayerId? ownerPlayerId, InputDevice? targetDevice)
        {
            PlayerController controller = Instantiate(playerControllerPrefab, transform);
            controller.Initialize(ownerPlayerId, targetDevice);
            return controller;
        }

        /// <summary>The render camera a possessed pawn should drive: <paramref name="controller"/>'s split-screen camera if split-screen is on, otherwise <see cref="mainCamera"/>.</summary>
        public Camera GetCameraFor(PlayerController controller)
        {
            if (_isSplitScreenEnabled && _splitScreenCameras.TryGetValue(controller, out Camera splitCamera))
                return splitCamera;

            return mainCamera;
        }

        [DebugCommand("Enable Split Screen")]
        public static void SetSplitScreenEnabled()
        {
            Instance?.EnableSplitScreen();
        }

        [DebugCommand("Disable Split Screen")]
        public static void SetSplitScreenDisabled()
        {
            Instance?.DisableSplitScreen();
        }

        /// <summary>Swaps the authored layout set used by <see cref="EnableSplitScreen"/>, so layouts can be changed at runtime.</summary>
        public void SetLayoutSet(SplitScreenLayoutSet newLayoutSet)
        {
            layoutSet = newLayoutSet;
        }

        /// <summary>
        /// Spawns one camera per entry in <see cref="ActiveControllers"/>, rect-ed according to its
        /// position in that list via <see cref="layoutSet"/>, and disables <see cref="mainCamera"/>.
        /// The global controller never gets a split-screen camera. The AudioListener is only ever
        /// active on the first player's camera.
        /// </summary>
        public void EnableSplitScreen()
        {
            DisableSplitScreen();

            int count = _activeControllers.Count;
            if (count == 0)
                return;

            SplitScreenLayoutSet.SplitScreenLayout[] layouts = layoutSet.LayoutsByPlayerCount;
            Rect[] rects = layouts[Mathf.Clamp(count, 1, layouts.Length) - 1].PlayerRects;

            for (int i = 0; i < count; i++)
            {
                Camera splitCamera = Instantiate(mainCamera, mainCamera.transform.parent);
                splitCamera.transform.parent = _activeControllers[i].transform;
                splitCamera.transform.localPosition = Vector3.zero;
                splitCamera.transform.localRotation = Quaternion.identity;

                splitCamera.gameObject.tag = "Untagged"; // keeps Camera.main unambiguous - mainCamera itself keeps the MainCamera tag
                splitCamera.rect = rects[Mathf.Min(i, rects.Length - 1)];

                AudioListener listener = splitCamera.GetComponent<AudioListener>();
                if (listener != null)
                    listener.enabled = i == 0;

                _splitScreenCameras[_activeControllers[i]] = splitCamera;
            }

            mainCamera.gameObject.SetActive(false);
            _isSplitScreenEnabled = true;
        }

        /// <summary>Destroys every spawned split-screen camera and re-activates <see cref="mainCamera"/>.</summary>
        public void DisableSplitScreen()
        {
            foreach (Camera splitCamera in _splitScreenCameras.Values)
            {
                if (splitCamera != null)
                    Destroy(splitCamera.gameObject);
            }

            _splitScreenCameras.Clear();
            _isSplitScreenEnabled = false;
            mainCamera.gameObject.SetActive(true);
        }
    }
}
