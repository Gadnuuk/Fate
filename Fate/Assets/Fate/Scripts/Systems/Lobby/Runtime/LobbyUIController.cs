using System.Collections.Generic;
using System.Linq;
using Fate.Systems.Content;
using Fate.Systems.Utilities;
using FishNet.Connection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fate.Systems.Lobby
{
    /// <summary>
    /// Pre-game lobby screen. Showing it never itself starts a connection - that's deliberately
    /// deferred until the host presses <see cref="_startHostingButton"/> (see
    /// <see cref="OnStartHostingClicked"/>), so opening this screen is purely a UI transition.
    ///
    /// Once hosting, lists who's connected grouped by connection - one connection can carry
    /// multiple local players (see <see cref="PartyRosterService"/>) - showing each connection's
    /// IP address and a Kick button that drops the whole connection (and every local player it
    /// declared) at once, plus a per-player row underneath for each local player it declared.
    ///
    /// The right side lets the host pick a <see cref="ContentBundle"/> (loaded from
    /// Resources/<see cref="ContentBundlesResourcesPath"/>) and launch everyone into it via
    /// <see cref="SessionManager.StartGame"/>, which is safe to call again later with a different
    /// bundle (e.g. a level transition) - this screen doesn't need to know or care about that.
    ///
    /// Lives in its own "lobby" content scene, loaded/unloaded as a <see cref="ContentBundle"/> by
    /// <see cref="ContentSceneManager"/> - e.g. when Host is clicked in the main menu. Loading and
    /// unloading the scene is what shows/hides this screen; there's no explicit Show()/Hide() here.
    /// <see cref="OnBackClicked"/> stops hosting (if active) and loads <see cref="mainMenuBundle"/>
    /// to return to the main menu.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LobbyUIController : PersistentSingleton<LobbyUIController>
    {
        // Every ContentBundle meant to be selectable from this screen must live somewhere under
        // this Resources folder (e.g. Assets/.../Resources/ContentBundles/Sandbox.asset).
        private const string ContentBundlesResourcesPath = "ContentBundles";

        [Tooltip("Loaded via ContentSceneManager when Back to Main Menu is pressed.")]
        [SerializeField] private ContentBundle mainMenuBundle;

        private VisualElement _root;
        private VisualElement _playerList;
        private Button _startHostingButton;
        private Button _stopHostingButton;
        private Button _startGameButton;
        private Button _backButton;
        private DropdownField _contentBundleDropdown;

        private readonly List<ContentBundle> _availableBundles = new();

        private bool _isHosting;

        private static SessionManager Session => SessionManager.Instance;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return; // duplicate instance - base.Awake() already queued it for Destroy()

            CacheElements();
        }

        private void CacheElements()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _playerList = _root.Q("player-list");
            _startHostingButton = _root.Q<Button>("start-hosting-button");
            _stopHostingButton = _root.Q<Button>("stop-hosting-button");
            _startGameButton = _root.Q<Button>("start-game-button");
            _backButton = _root.Q<Button>("back-to-main-menu-button");
            _contentBundleDropdown = _root.Q<DropdownField>("content-bundle-dropdown");
        }

        private void OnEnable()
        {
            if (_root == null)
                return; // duplicate instance being destroyed - Awake() bailed before querying elements

            // UIDocument rebuilds its visual tree from the source UXML whenever this GameObject is
            // disabled and re-enabled, discarding any runtime style changes (e.g. the hosting-button
            // visibility SetHosting toggles below) and leaving both Start/Stop Hosting buttons at
            // their UXML-default visible state. Re-querying here keeps our cached references pointed
            // at the live tree, and reapplying SetHosting/SetVisible restores whatever state should
            // actually be showing instead of that default.
            CacheElements();

            _startHostingButton.clicked += OnStartHostingClicked;
            _stopHostingButton.clicked += OnStopHostingClicked;
            _startGameButton.clicked += OnStartGameClicked;
            _backButton.clicked += OnBackClicked;

            RefreshContentBundleDropdown();
            SetHosting(_isHosting);
            if (_isHosting)
                RefreshPlayerList(); // rebuild rows into the freshly-cloned player-list container
            else
                _playerList.Clear();
        }

        private void OnDisable()
        {
            if (_root == null)
                return;

            _startHostingButton.clicked -= OnStartHostingClicked;
            _stopHostingButton.clicked -= OnStopHostingClicked;
            _startGameButton.clicked -= OnStartGameClicked;
            _backButton.clicked -= OnBackClicked;

            UnsubscribeFromRoster();
        }

        private void OnStartHostingClicked()
        {
            Session.StartHost();
            SubscribeToRoster();
            SetHosting(true);
            RefreshPlayerList();
        }

        private void OnStopHostingClicked() => StopHosting();

        private void StopHosting()
        {
            Session.LeaveLobby();
            UnsubscribeFromRoster();
            SetHosting(false);
            _playerList.Clear();
        }

        /// <summary>Stops hosting (if currently active) and returns to the main menu by loading <see cref="mainMenuBundle"/>.</summary>
        private void OnBackClicked()
        {
            if (_isHosting)
                StopHosting();

            ContentSceneManager.LoadBundle(mainMenuBundle);
        }

        private void SetHosting(bool hosting)
        {
            _isHosting = hosting;
            SetVisible(_startHostingButton, !hosting);
            SetVisible(_stopHostingButton, hosting);
            SetVisible(_startGameButton, hosting); // nothing to load scenes into without a running server
        }

        private void SubscribeToRoster()
        {
            if (PartyRosterService.Instance == null)
                return;

            PartyRosterService.Instance.RosterReceived += OnRosterChanged;
            PartyRosterService.Instance.RosterRemoved += OnRosterRemoved;
        }

        private void UnsubscribeFromRoster()
        {
            if (PartyRosterService.Instance == null)
                return;

            PartyRosterService.Instance.RosterReceived -= OnRosterChanged;
            PartyRosterService.Instance.RosterRemoved -= OnRosterRemoved;
        }

        private void OnRosterChanged(NetworkConnection conn, PartyRosterService.ConnectionRoster roster) => RefreshPlayerList();

        private void OnRosterRemoved(NetworkConnection conn) => RefreshPlayerList();

        // Full rebuild on every change rather than incremental patching - same idiom as
        // MainMenuFlowController.RefreshProfileList. The roster list is small (a handful of
        // connections at most), so this is cheap.
        private void RefreshPlayerList()
        {
            _playerList.Clear();

            if (PartyRosterService.Instance == null)
                return;

            foreach (KeyValuePair<NetworkConnection, PartyRosterService.ConnectionRoster> pair in PartyRosterService.Instance.AllRosters)
                _playerList.Add(BuildConnectionGroup(pair.Key, pair.Value));
        }

        // One group per connection: an IP + Kick header (kicking drops the whole connection, taking
        // every local player it declared with it - PartyRosterService.RosterRemoved then refreshes
        // this list the same way a voluntary disconnect would), then one label per local player.
        private VisualElement BuildConnectionGroup(NetworkConnection conn, PartyRosterService.ConnectionRoster roster)
        {
            var group = new VisualElement();
            group.AddToClassList("connection-group");

            var header = new VisualElement();
            header.AddToClassList("connection-group__header");

            var ipLabel = new Label(conn.GetAddress());
            ipLabel.AddToClassList("connection-group__ip");
            header.Add(ipLabel);

            var kickButton = new Button(() => OnKickClicked(conn)) { text = "Kick" };
            kickButton.AddToClassList("connection-group__kick-button");
            header.Add(kickButton);

            group.Add(header);

            foreach (LocalPlayerEntry entry in roster.LocalPlayers)
            {
                var playerLabel = new Label(entry.DisplayName);
                playerLabel.AddToClassList("player-entry");
                group.Add(playerLabel);
            }

            return group;
        }

        private void OnKickClicked(NetworkConnection conn) => conn.Disconnect(true);

        private void RefreshContentBundleDropdown()
        {
            _availableBundles.Clear();
            _availableBundles.AddRange(Resources.LoadAll<ContentBundle>(ContentBundlesResourcesPath));

            _contentBundleDropdown.choices = _availableBundles.Select(b => b.name).ToList();
            _contentBundleDropdown.index = _availableBundles.Count > 0 ? 0 : -1;
        }

        private void OnStartGameClicked()
        {
            int index = _contentBundleDropdown.index;
            if (index < 0 || index >= _availableBundles.Count)
            {
                Debug.LogWarning($"{nameof(LobbyUIController)}: no content bundle selected - cannot start the game.", this);
                return;
            }

            Session.StartGame(_availableBundles[index]);
        }

        private static void SetVisible(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
