using System.Collections.Generic;
using Fate.Systems.Utilities;
using UnityEngine;

namespace Fate.Systems.Content
{
    /// <summary>
    /// A base level scene (geo, lighting, collision) plus scenes layered on top of it
    /// for a specific game mode. Loaded as a unit by ContentSceneManager.
    /// </summary>
    [CreateAssetMenu(fileName = "ContentBundle", menuName = "Fate/Content/Content Bundle")]
    public class ContentBundle : ScriptableObject
    {
        [Tooltip("Base level scene: geo, lighting, collision.")]
        [SerializeField] private SceneReference baseScene;

        [Tooltip("Scenes layered on top of the base scene for a specific game mode.")]
        [SerializeField] private List<SceneReference> layers = new();

        public SceneReference BaseScene => baseScene;
        public IReadOnlyList<SceneReference> Layers => layers;
    }
}
