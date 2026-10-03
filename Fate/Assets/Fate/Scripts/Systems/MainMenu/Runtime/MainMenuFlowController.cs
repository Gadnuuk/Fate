using System.Collections.Generic;
using System.Linq;
using Fate.Systems.Game;
using Fate.Systems.Player;
using Fate.Systems.PlayerConfig;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fate.Systems.MainMenu
{
    /// <summary>
    /// Drives the three-screen main menu flow (init -> login -> main menu). Login is reused both
    /// for the mandatory first login and for adding an optional second profile from the main
    /// menu, overlaying on top of whatever is currently shown. The list of profiles logged in for
    /// this session is owned by <see cref="ProfileManager"/>, not this controller.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuFlowController : MonoBehaviour
    {
        // ProfileManager is a persistent singleton (see PersistentSingleton<T>) placed by the boot
        // sequence - it must outlive this menu's scene so Fate.Systems.Lobby.LobbySession can read
        // the same logged-in profiles later. This controller only ever reads Instance; it never
        // spawns one itself.
        private ProfileManager _profileManager;

        // Cached once in Awake rather than re-reading PlayerControllerConfigWindow.Instance in
        // OnEnable/OnDisable/OnConfigureControllersClicked: PersistentSingleton<T>.Instance nulls
        // its backing field in OnDestroy, so a second access after the real instance is torn down
        // (e.g. OnDisable firing during exit-play-mode shutdown, after the window's own OnDestroy
        // already ran) would lazily spawn a throwaway replacement GameObject with no UIDocument
        // source assigned - that one NREs in its own Awake querying for "confirm-button". Caching
        // the reference once means teardown only ever unsubscribes from the real instance (or a
        // Unity "fake null" if it's already gone, which the null-check below handles safely).
        private PlayerControllerConfigWindow _configWindow;

        private VisualElement _initScreen;
        private VisualElement _mainMenuScreen;
        private VisualElement _loginScreen;
        private VisualElement _profileSelectList;
        private VisualElement _profileList;

        private Button _joinButton;
        private Button _addProfileButton;
        private Button _createProfileButton;
        private Button _loginCancelButton;
        private Button _modeQuickPlayButton;
        private Button _modeCustomMatchButton;
        private Button _modeSandboxButton;
        private Button _modeJoinPartyButton;
        private Button _configureControllersButton;
        private Button _optionsButton;
        private Button _creditsButton;
        private TextField _newProfileNameField;

        private bool _isFirstLogin;

        private void Awake()
        {
            _profileManager = ProfileManager.Instance;
            _configWindow = PlayerControllerConfigWindow.Instance;

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            _initScreen = root.Q("init-screen");
            _mainMenuScreen = root.Q("main-menu-screen");
            _loginScreen = root.Q("login-screen");
            _profileSelectList = root.Q("profile-select-list");
            _profileList = root.Q("profile-list");

            _joinButton = root.Q<Button>("join-button");
            _addProfileButton = root.Q<Button>("add-profile-button");
            _createProfileButton = root.Q<Button>("create-profile-button");
            _loginCancelButton = root.Q<Button>("login-cancel-button");
            _modeQuickPlayButton = root.Q<Button>("mode-quickplay-button");
            _modeCustomMatchButton = root.Q<Button>("mode-custommatch-button");
            _modeSandboxButton = root.Q<Button>("mode-sandbox-button");
            _modeJoinPartyButton = root.Q<Button>("mode-joinparty-button");
            _configureControllersButton = root.Q<Button>("configure-controllers-button");
            _optionsButton = root.Q<Button>("options-button");
            _creditsButton = root.Q<Button>("credits-button");
            _newProfileNameField = root.Q<TextField>("new-profile-name-field");
        }

        private void OnEnable()
        {
            _joinButton.clicked += OnJoinClicked;
            _addProfileButton.clicked += OnAddProfileClicked;
            _createProfileButton.clicked += OnCreateProfileClicked;
            _loginCancelButton.clicked += OnLoginCancelClicked;
            _modeQuickPlayButton.clicked += () => Debug.Log("Quick Play - not yet implemented.");
            _modeCustomMatchButton.clicked += () => Debug.Log("Custom Match - not yet implemented.");
            _modeSandboxButton.clicked += () => Debug.Log("Sandbox - not yet implemented.");
            _modeJoinPartyButton.clicked += () => Debug.Log("Join Party - not yet implemented.");
            _configureControllersButton.clicked += OnConfigureControllersClicked;
            _optionsButton.clicked += () => Debug.Log("Options - not yet implemented.");
            _creditsButton.clicked += () => Debug.Log("Credits - not yet implemented.");
            _configWindow.Closed += OnConfigureControllersClosed;

            SetVisible(_initScreen, true);
            SetVisible(_mainMenuScreen, false);
            SetVisible(_loginScreen, false);
        }

        private void OnDisable()
        {
            _joinButton.clicked -= OnJoinClicked;
            _addProfileButton.clicked -= OnAddProfileClicked;
            _createProfileButton.clicked -= OnCreateProfileClicked;
            _loginCancelButton.clicked -= OnLoginCancelClicked;
            _configureControllersButton.clicked -= OnConfigureControllersClicked;

            if (_configWindow != null)
                _configWindow.Closed -= OnConfigureControllersClosed;
        }

        private void OnJoinClicked()
        {
            SetVisible(_initScreen, false);
            OpenLogin(isFirstLogin: true);
        }

        private void OnAddProfileClicked()
        {
            OpenLogin(isFirstLogin: false);
        }

        // Same input-isolation concern as OnConfigureControllersClicked below: the main-menu screen's
        // buttons stay in UI Toolkit's focus/navigation ring unless hidden, so gamepad up/down meant
        // for the login screen was also reaching them underneath. Hiding covers both the mandatory
        // first login (already hidden via the init screen, this is a no-op there) and add-profile.
        private void OpenLogin(bool isFirstLogin)
        {
            _isFirstLogin = isFirstLogin;
            SetVisible(_mainMenuScreen, false);
            SetVisible(_loginCancelButton, !isFirstLogin);
            RefreshProfileSelectList();
            SetVisible(_loginScreen, true);
        }

        private void OnLoginCancelClicked()
        {
            SetVisible(_loginScreen, false);
            SetVisible(_mainMenuScreen, true);
        }

        // PlayerControllerConfigWindow is its own persistent singleton (see PersistentSingleton<T>) -
        // _configWindow was cached from Instance once in Awake (see its field declaration for why).
        //
        // The main-menu screen is hidden (not just left underneath) while the config window is open:
        // its buttons stay visible-and-focusable via UI Toolkit's default navigation/Submit handling
        // otherwise, so keyboard/gamepad input meant for the config window was also reaching this
        // screen's buttons at the same time. Hiding it removes it from the focus ring entirely.
        private void OnConfigureControllersClicked()
        {
            SetVisible(_mainMenuScreen, false);
            _configWindow.Show();
        }

        private void OnConfigureControllersClosed()
        {
            SetVisible(_mainMenuScreen, true);
            RefreshProfileList(); // device assignments may have just changed - refresh the per-player device labels
        }

        private void OnCreateProfileClicked()
        {
            string displayName = _newProfileNameField.value?.Trim();
            if (string.IsNullOrEmpty(displayName))
            {
                Debug.LogWarning($"{nameof(MainMenuFlowController)}: enter a name before creating a profile.");
                return;
            }

            PlayerProfile profile = _profileManager.CreateProfile(displayName);
            _newProfileNameField.value = string.Empty;
            LoginProfile(profile);
        }

        private void RefreshProfileSelectList()
        {
            _profileSelectList.Clear();

            IEnumerable<PlayerProfile> selectable = _profileManager.LoadProfiles()
                .Where(saved => _profileManager.LoggedInProfiles.All(loggedIn => loggedIn.PlayerId != saved.PlayerId));

            foreach (PlayerProfile profile in selectable)
            {
                var button = new Button(() => LoginProfile(profile)) { text = profile.PlayerId.Tag };
                button.AddToClassList("menu-button");
                _profileSelectList.Add(button);
            }
        }

        private void LoginProfile(PlayerProfile profile)
        {
            _profileManager.LogIn(profile);

            RefreshProfileList();
            SetVisible(_loginScreen, false);
            SetVisible(_mainMenuScreen, true);
        }

        // Shows each logged-in profile's tag, plus - if PlayerControllerManager currently has a
        // spawned, non-global PlayerController owned by that profile (i.e. the player went through
        // PlayerControllerConfigWindow and confirmed a device for them) - the device it's bound to.
        // Profiles that haven't been assigned a controller yet just show their tag alone.
        private void RefreshProfileList()
        {
            _profileList.Clear();

            IReadOnlyList<PlayerController> activeControllers = PlayerControllerManager.Instance.ActiveControllers;

            foreach (PlayerProfile profile in _profileManager.LoggedInProfiles)
            {
                var entry = new VisualElement();
                entry.AddToClassList("profile-entry");

                var nameLabel = new Label(profile.PlayerId.Tag);
                nameLabel.AddToClassList("profile-entry__name");
                entry.Add(nameLabel);

                PlayerController controller = activeControllers
                    .FirstOrDefault(c => c.OwnerPlayerId == profile.PlayerId);

                if (controller != null && controller.TargetDevice.HasValue)
                {
                    var deviceLabel = new Label(controller.TargetDevice.Value.ToString());
                    deviceLabel.AddToClassList("profile-entry__device");
                    entry.Add(deviceLabel);
                }

                _profileList.Add(entry);
            }
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
