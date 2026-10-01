using System.Collections.Generic;
using Fate.Systems.Content;
using UnityEngine;

namespace Fate.Systems.Boot
{
    [CreateAssetMenu(fileName = "BootSequence", menuName = "Fate/Boot/Boot Sequence")]
    public class BootSequence : ScriptableObject
    {
        [Tooltip("Scenes to load additively, in order.")]
        [SerializeField] private List<BootSequenceEntry> entries = new();

        [Tooltip("Content bundle to load last, once every scene above has finished loading.")]
        [SerializeField] private ContentBundle contentBundle;

        public IReadOnlyList<BootSequenceEntry> Entries => entries;
        public ContentBundle ContentBundle => contentBundle;
    }
}
