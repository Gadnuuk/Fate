using System.Collections.Generic;
using Fate.Systems.InputActions;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// The set of <see cref="InputAction"/>s that are meaningful in a given mode (gameplay, a
    /// menu, etc). A <see cref="PlayerController"/> filters the global <see cref="InputManager"/>
    /// through whichever context is currently active, so it only relays/queries actions that
    /// belong to it.
    /// </summary>
    [CreateAssetMenu(fileName = "InputContext", menuName = "Fate/Input/Input Context")]
    public class InputContext : ScriptableObject
    {
        [SerializeField]
        private List<InputAction> actions = new();

        private HashSet<InputAction> _lookup;

        public bool Contains(InputAction action)
        {
            if (action == null)
                return false;

            _lookup ??= new HashSet<InputAction>(actions);
            return _lookup.Contains(action);
        }
    }
}
