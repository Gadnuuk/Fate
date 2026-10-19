using Fate.Systems.Content;
using Fate.Systems.Lobby;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fate.Systems.DeveloperMenu
{
    /// <summary>
    /// Toggleable overlay (default key: ~) listing every <see cref="DebugCommandAttribute"/>-tagged
    /// command as a button, plus a few manually-wired buttons (e.g. <see cref="mainMenuBundle"/>)
    /// for jumping straight to a specific place without going through normal UI flow. Lives in the
    /// always-loaded debug_ui boot scene so it can be stripped from non-dev builds by removing that
    /// scene from the boot sequence.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DeveloperMenuController : MonoBehaviour
    {
        [SerializeField]
        private KeyCode toggleKey = KeyCode.BackQuote;

        [Tooltip("Loaded via ContentSceneManager by the manually-wired \"Load Main Menu\" button.")]
        [SerializeField]
        private ContentBundle mainMenuBundle;

        private VisualElement _panel;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _panel = root.Q("developer-menu-panel");

            VisualElement commandList = root.Q("command-list");
            foreach (DebugCommandEntry command in DebugCommandRegistry.Commands)
            {
                var button = new Button(command.Invoke) { text = command.Label };
                button.AddToClassList("menu-button");
                commandList.Add(button);
            }

            var loadMainMenuButton = new Button(OnLoadMainMenuClicked) { text = "Load Main Menu" };
            loadMainMenuButton.AddToClassList("menu-button");
            commandList.Add(loadMainMenuButton);

            SetVisible(false);
        }

        // Leaves whatever game session is active (safe to call even when there isn't one - see
        // SessionManager.LeaveGame) before switching content, so this button works both from the
        // lobby and from gameplay - either way FishNet's global scenes get torn down first, rather
        // than just piling the main menu's locally-loaded scenes on top of them.
        private void OnLoadMainMenuClicked()
        {
            SessionManager.Instance?.LeaveGame();
            ContentSceneManager.LoadBundle(mainMenuBundle);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                SetVisible(_panel.style.display == DisplayStyle.None);
        }

        private void SetVisible(bool visible)
        {
            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
