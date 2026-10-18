using System.Collections.Generic;
using UnityEngine;

namespace Fate.Systems.Boot
{
    /// <summary>
    /// Scenes to load additively, in order, to bring the game up. The main menu is just the last
    /// entry here, not a <see cref="Fate.Systems.Content.ContentBundle"/> - content bundles are
    /// reserved for gameplay content loaded later (see <see cref="Fate.Systems.Lobby.SessionManager"/>),
    /// not for boot-time scenes.
    /// </summary>
    [CreateAssetMenu(fileName = "BootSequence", menuName = "Fate/Boot/Boot Sequence")]
    public class BootSequence : ScriptableObject
    {
        [Tooltip("Scenes to load additively, in order.")]
        [SerializeField] private List<BootSequenceEntry> entries = new();

        public IReadOnlyList<BootSequenceEntry> Entries => entries;
    }
}
