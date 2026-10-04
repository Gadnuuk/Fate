using System.Collections.Generic;
using Fate.Systems.Game;
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

        [Tooltip("Server-side pawn spawner for this bundle's gameplay (see GameModeBase). Instantiated into the gameplay scene once it finishes loading on the server; left unset for bundles that never host gameplay (e.g. a menu/lobby bundle).")]
        [SerializeField] private GameModeBase gameModePrefab;

        public SceneReference BaseScene => baseScene;
        public IReadOnlyList<SceneReference> Layers => layers;
        public GameModeBase GameModePrefab => gameModePrefab;
    }
}
