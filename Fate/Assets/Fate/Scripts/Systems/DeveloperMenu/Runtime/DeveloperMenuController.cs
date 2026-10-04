using UnityEngine;
using UnityEngine.UIElements;

namespace Fate.Systems.DeveloperMenu
{
    /// <summary>
    /// Toggleable overlay (default key: ~) listing every <see cref="DebugCommandAttribute"/>-tagged
    /// command as a button. Lives in the always-loaded debug_ui boot scene so it can be stripped
    /// from non-dev builds by removing that scene from the boot sequence.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DeveloperMenuController : MonoBehaviour
    {
        [SerializeField]
        private KeyCode toggleKey = KeyCode.BackQuote;

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

            SetVisible(false);
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
