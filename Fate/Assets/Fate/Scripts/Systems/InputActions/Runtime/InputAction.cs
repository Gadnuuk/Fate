using UnityEngine;

namespace Fate.Systems.InputActions
{
    /// <summary>
    /// An identifier for a gameplay-meaningful input ("Jump", "MenuUp"), decoupled from any
    /// specific key or button. What actually drives it lives on an <see cref="InputConfig"/>'s
    /// <see cref="InputDefinition"/>; which ones a given player currently responds to is filtered
    /// by their active <c>InputContext</c>. Components bind to these directly and never need to
    /// know about contexts.
    /// </summary>
    [CreateAssetMenu(fileName = "InputAction", menuName = "Fate/Input/Input Action")]
    public class InputAction : ScriptableObject
    {
        public string ActionName = "";
    }
}
