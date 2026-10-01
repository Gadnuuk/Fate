using System;
using Fate.Systems.Utilities;
using UnityEngine;

namespace Fate.Systems.Boot
{
    [Serializable]
    public class BootSequenceEntry
    {
        [SerializeField] private SceneReference scene;

        [Tooltip("If true, this scene is unloaded once the next scene in the sequence starts loading.")]
        [SerializeField] private bool unloadWhenNextSceneStarts;

        public SceneReference Scene => scene;
        public bool UnloadWhenNextSceneStarts => unloadWhenNextSceneStarts;
    }
}
