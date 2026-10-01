using System.Collections.Generic;
using System.Linq;
using Fate.Systems.Player;
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
        private Button _optionsButton;
        private Button _creditsButton;
        private TextField _newProfileNameField;

        private bool _isFirstLogin;

        private void Awake()
        {
            _profileManager = ProfileManager.Instance;

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
            _optionsButton.clicked += () => Debug.Log("Options - not yet implemented.");
            _creditsButton.clicked += () => Debug.Log("Credits - not yet implemented.");

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

        private void OpenLogin(bool isFirstLogin)
        {
            _isFirstLogin = isFirstLogin;
            SetVisible(_loginCancelButton, !isFirstLogin);
            RefreshProfileSelectList();
            SetVisible(_loginScreen, true);
        }

        private void OnLoginCancelClicked()
        {
            SetVisible(_loginScreen, false);
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

        private void RefreshProfileList()
        {
            _profileList.Clear();

            foreach (PlayerProfile profile in _profileManager.LoggedInProfiles)
            {
                var label = new Label(profile.PlayerId.Tag);
                label.AddToClassList("profile-entry");
                _profileList.Add(label);
            }
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
