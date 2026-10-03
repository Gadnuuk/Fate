using System.Collections.Generic;
using System.Linq;
using Fate.Systems.Game;
using Fate.Systems.InputActions;
using Fate.Systems.Player;
using Fate.Systems.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fate.Systems.PlayerConfig
{
    /// <summary>
    /// Standalone runtime screen for claiming input devices into logged-in profiles' columns
    /// before handing the result to <see cref="PlayerControllerManager"/>. Reads input through
    /// the manager's <see cref="PlayerController.GlobalController"/> API (device-qualified events
    /// and queries) rather than possessing a pawn - this window drives itself.
    ///
    /// Fully gamepad/keyboard navigable with no mouse required, and deliberately driven end to end
    /// by the custom input system rather than UI Toolkit's own focus/navigation - the whole point of
    /// this screen is "which physical device did that," which UI Toolkit's device-agnostic
    /// Navigation* events can't express at all. The one UI Toolkit widget here (<see cref="_confirmButton"/>)
    /// has its own keyboard/gamepad navigation explicitly disabled (see <see cref="Awake"/>) so Return
    /// doesn't double-fire through both systems at once; mouse clicks on it still work as a convenience,
    /// same as clicking a placed icon to release it.
    ///
    /// Per device, two states - unclaimed (sitting in the pool) or claimed (sitting in exactly one
    /// column). <see cref="moveAction"/> is the only way to change either: while unclaimed, a primed
    /// move claims the first open column scanning from the edge matching the direction pressed (right
    /// scans left-to-right, left scans right-to-left) - a no-op if every column is already occupied,
    /// which is how "can't move onto a claimed player" falls out naturally. While already claimed, a
    /// primed move steps to the adjacent column in that direction - if that column is open it moves
    /// there, if it's occupied it's blocked (no-op), and if there IS no adjacent column (already at
    /// the first/last column and moving further off that end) it releases back to the pool instead.
    /// <see cref="confirmAction"/> isn't per-device at all anymore: any device pressing it submits the
    /// whole configuration immediately, same as clicking <see cref="_confirmButton"/>.
    ///
    /// Persistent singleton (see <see cref="PersistentSingleton{T}"/>), same pattern as
    /// <see cref="PlayerControllerManager"/>/<see cref="ProfileManager"/> - callers anywhere reach
    /// it via <see cref="PersistentSingleton{T}.Instance"/> and call <see cref="Show"/>. Preferred
    /// setup is still placing one instance (with its <see cref="UIDocument"/> source asset/panel
    /// settings configured) in a boot-sequence scene; the base class's lazy create-on-first-access
    /// is only a backstop and won't have a visual tree assigned.
    ///
    /// Visibility toggles the root <see cref="VisualElement"/>'s display style rather than
    /// <see cref="GameObject.SetActive"/>, so the GameObject - and this singleton - stays active
    /// and discoverable even while the screen is hidden.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerControllerConfigWindow : PersistentSingleton<PlayerControllerConfigWindow>
    {
        private const float MoveDeadzone = 0.5f;

        private static readonly InputDevice[] GamepadDevices =
        {
            InputDevice.Gamepad1, InputDevice.Gamepad2, InputDevice.Gamepad3, InputDevice.Gamepad4,
        };

        [SerializeField]
        private InputAction moveAction;

        [SerializeField]
        private InputAction confirmAction;

        /// <summary>
        /// Raised when this screen closes (confirm or any other dismissal path), so whatever opened
        /// it - e.g. <see cref="Fate.Systems.MainMenu.MainMenuFlowController"/> - can restore its own
        /// screen. This window doesn't know or care who opened it; callers own that relationship.
        /// </summary>
        public event System.Action Closed;

        private VisualElement _root;
        private VisualElement _columnsContainer;
        private VisualElement _poolContainer;
        private Button _confirmButton;

        private readonly List<VisualElement> _columnSlots = new();
        private readonly List<PlayerProfile> _columnProfiles = new();
        private readonly List<DeviceSlot> _deviceSlots = new();

        private bool _isOpen;

        private PlayerController GlobalController => PlayerControllerManager.Instance.GlobalController;

        private class DeviceSlot
        {
            public InputDevice Device;
            public VisualElement Icon;
            public int ColumnIndex = -1;
            public bool MovePrimed = true;
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return; // duplicate instance - base.Awake() already queued it for Destroy()

            _root = GetComponent<UIDocument>().rootVisualElement;
            _columnsContainer = _root.Q("player-columns");
            _poolContainer = _root.Q("device-pool");
            _confirmButton = _root.Q<Button>("confirm-button");

            // Keyboard Return (and gamepad A) already submits via confirmAction below, device-qualified.
            // Leaving this button focusable would let UI Toolkit's own default Submit navigation fire
            // it a second time off the same keypress whenever it happens to hold focus. Mouse clicks
            // still work fine - ClickEvent doesn't go through focus/navigation at all.
            _confirmButton.focusable = false;

            SetVisible(false);
        }

        private void OnEnable()
        {
            if (_confirmButton == null)
                return; // duplicate instance being destroyed - Awake() bailed before querying elements

            _confirmButton.clicked += OnConfirmClicked;
            GlobalController.ActionPressedForDevice += OnActionPressedForDevice;
        }

        private void OnDisable()
        {
            if (_confirmButton == null)
                return;

            _confirmButton.clicked -= OnConfirmClicked;
            GlobalController.ActionPressedForDevice -= OnActionPressedForDevice;
        }

        private void Update()
        {
            if (!_isOpen || moveAction == null)
                return;

            foreach (DeviceSlot slot in _deviceSlots)
            {
                float axis = GlobalController.GetAxisForDevice(moveAction, slot.Device);

                if (Mathf.Abs(axis) < MoveDeadzone)
                {
                    slot.MovePrimed = true;
                    continue;
                }

                if (!slot.MovePrimed)
                    continue;

                slot.MovePrimed = false;

                if (slot.ColumnIndex < 0)
                    TryClaimClosestColumn(slot, axis > 0f ? 1 : -1);
                else
                    TryMoveOrRelease(slot, axis > 0f ? 1 : -1);
            }
        }

        /// <summary>Opens the screen, rebuilding columns/device pool from current session state.</summary>
        public void Show()
        {
            _isOpen = true;
            BuildColumns();
            BuildDevicePool();
            SetVisible(true);
        }

        /// <summary>Closes the screen.</summary>
        public void Hide()
        {
            _isOpen = false;
            SetVisible(false);
            Closed?.Invoke();
        }

        private void SetVisible(bool visible) => _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private void BuildColumns()
        {
            _columnsContainer.Clear();
            _columnSlots.Clear();
            _columnProfiles.Clear();

            foreach (PlayerProfile profile in ProfileManager.Instance.LoggedInProfiles)
            {
                var column = new VisualElement();
                column.AddToClassList("player-column");

                var header = new Label(profile.PlayerId.Tag);
                header.AddToClassList("player-column__header");

                var slot = new VisualElement();
                slot.AddToClassList("player-column__slot");

                column.Add(header);
                column.Add(slot);
                _columnsContainer.Add(column);

                _columnSlots.Add(slot);
                _columnProfiles.Add(profile);
            }
        }

        private void BuildDevicePool()
        {
            _poolContainer.Clear();
            _deviceSlots.Clear();

            AddDeviceSlot(InputDevice.Keyboard, "Keyboard");

            string[] joystickNames = Input.GetJoystickNames();
            for (int i = 0; i < joystickNames.Length && i < GamepadDevices.Length; i++)
            {
                if (!string.IsNullOrEmpty(joystickNames[i]))
                    AddDeviceSlot(GamepadDevices[i], GamepadDevices[i].ToString());
            }
        }

        private void AddDeviceSlot(InputDevice device, string label)
        {
            var icon = new Label(label);
            icon.AddToClassList("device-icon");
            icon.RegisterCallback<ClickEvent>(_ => OnIconClicked(device));

            _poolContainer.Add(icon);
            _deviceSlots.Add(new DeviceSlot { Device = device, Icon = icon });
        }

        // confirmAction is global now, not per-device - whichever device presses it just submits
        // the whole screen, same as clicking _confirmButton. Device identity doesn't matter here.
        private void OnActionPressedForDevice(InputAction action, InputDevice device)
        {
            if (!_isOpen || action != confirmAction)
                return;

            OnConfirmClicked();
        }

        /// <summary>
        /// Claims the first open column scanning from the edge matching <paramref name="direction"/>
        /// (positive/right scans left-to-right, negative/left scans right-to-left) - a no-op if every
        /// column is already occupied.
        /// </summary>
        private void TryClaimClosestColumn(DeviceSlot slot, int direction)
        {
            int start = direction > 0 ? 0 : _columnSlots.Count - 1;
            int step = direction > 0 ? 1 : -1;

            for (int i = start; i >= 0 && i < _columnSlots.Count; i += step)
            {
                if (!IsColumnOccupied(i))
                {
                    PlaceSlotInColumn(slot, i);
                    return;
                }
            }
        }

        /// <summary>
        /// Steps a claimed slot to the adjacent column in <paramref name="direction"/>: moves there if
        /// open, no-ops if that column is occupied (blocked, not swapped), and releases back to the
        /// pool if there's no adjacent column at all (already at that end of the row).
        /// </summary>
        private void TryMoveOrRelease(DeviceSlot slot, int direction)
        {
            int targetIndex = slot.ColumnIndex + direction;

            if (targetIndex < 0 || targetIndex >= _columnSlots.Count)
            {
                ReleaseSlot(slot);
                return;
            }

            if (!IsColumnOccupied(targetIndex))
                PlaceSlotInColumn(slot, targetIndex);
        }

        private void PlaceSlotInColumn(DeviceSlot slot, int columnIndex)
        {
            slot.ColumnIndex = columnIndex;
            _columnSlots[columnIndex].Add(slot.Icon);
        }

        private void ReleaseSlot(DeviceSlot slot)
        {
            slot.ColumnIndex = -1;
            _poolContainer.Add(slot.Icon);
        }

        private void OnIconClicked(InputDevice device)
        {
            DeviceSlot slot = FindSlot(device);
            if (slot == null || slot.ColumnIndex < 0)
                return;

            ReleaseSlot(slot);
        }

        private void OnConfirmClicked()
        {
            IReadOnlyList<PlayerProfile> loggedIn = ProfileManager.Instance.LoggedInProfiles;
            var assignments = new List<PlayerControllerAssignment>();

            for (int i = 0; i < _columnSlots.Count; i++)
            {
                PlayerProfile profile = _columnProfiles[i];
                if (!loggedIn.Any(p => p.PlayerId == profile.PlayerId))
                    continue;

                DeviceSlot slot = FindSlotInColumn(i);
                if (slot != null)
                    assignments.Add(new PlayerControllerAssignment(profile.PlayerId, slot.Device));
            }

            PlayerControllerManager.Instance.ApplyConfiguration(assignments);
            Hide();
        }

        private bool IsColumnOccupied(int columnIndex) => _deviceSlots.Any(slot => slot.ColumnIndex == columnIndex);

        private DeviceSlot FindSlot(InputDevice device) => _deviceSlots.FirstOrDefault(slot => slot.Device == device);

        private DeviceSlot FindSlotInColumn(int columnIndex) => _deviceSlots.FirstOrDefault(slot => slot.ColumnIndex == columnIndex);
    }
}
