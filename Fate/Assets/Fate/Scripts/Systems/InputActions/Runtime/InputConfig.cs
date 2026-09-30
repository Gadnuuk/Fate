using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fate.Systems.InputActions
{
    [Serializable]
    public class InputBinding
    {
        public InputAction Action;
        public InputDefinition Definition = new();
    }

    /// <summary>The full set of raw-input-to-<see cref="InputAction"/> bindings that <see cref="InputManager"/> listens for.</summary>
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Fate/Input/Input Config")]
    public class InputConfig : ScriptableObject
    {
        [SerializeField]
        private List<InputBinding> bindings = new();

        public IReadOnlyList<InputBinding> Bindings => bindings;
    }
}
