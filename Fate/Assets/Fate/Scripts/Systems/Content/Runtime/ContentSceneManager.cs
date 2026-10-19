using System.Collections;
using System.Collections.Generic;
using Fate.Systems.Game;
using Fate.Systems.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fate.Systems.Content
{
    /// <summary>
    /// Persistent singleton that loads and unloads content bundles (a base level scene
    /// plus its game-mode layer scenes). Call LoadBundle to swap out whatever content is
    /// currently loaded - the previous bundle's scenes are unloaded first. Lives in the
    /// main_content scene (the last entry in the boot sequence) and loads
    /// <see cref="defaultBundle"/> on <see cref="Start"/> so there's always something
    /// loaded (the main menu, by default) without any other system having to kick it off.
    /// </summary>
    public class ContentSceneManager : PersistentSingleton<ContentSceneManager>
    {
        [Tooltip("Loaded automatically on Start - typically the main menu. This is how the main menu (and anything else reached the same way) gets into the world without the boot sequence needing to know about content bundles.")]
        [SerializeField] private ContentBundle defaultBundle;

        private static readonly List<Scene> LoadedScenes = new();

        private Coroutine _loadCoroutine;

        public static ContentBundle ActiveBundle { get; private set; }

        private void Start()
        {
            if (defaultBundle != null)
                LoadBundle(defaultBundle);
        }

        public static void LoadBundle(ContentBundle bundle)
        {
            if (Instance == null)
            {
                Debug.LogError($"{nameof(ContentSceneManager)} has no active instance in the scene.");
                return;
            }

            if (bundle == null || bundle.BaseScene == null || !bundle.BaseScene.IsAssigned)
            {
                Debug.LogError($"{nameof(ContentSceneManager)} received an unassigned {nameof(ContentBundle)}.");
                return;
            }

            // Cancel any load/unload already in flight before starting the next one - same guard
            // BootSequenceManager.Reboot uses - so a second LoadBundle call (e.g. a double-clicked
            // button) can't race the first one's UnloadLoadedScenes/LoadSceneIntoBundle calls and
            // leave two bundles' scenes loaded at once.
            if (Instance._loadCoroutine != null)
                Instance.StopCoroutine(Instance._loadCoroutine);

            Instance._loadCoroutine = Instance.StartCoroutine(Instance.LoadBundleRoutine(bundle));
        }

        /// <summary>
        /// Unloads whatever bundle is currently loaded without loading a replacement. Used by
        /// <see cref="Fate.Systems.Lobby.SessionManager"/> so each peer's locally-loaded lobby
        /// content scene (loaded via LoadBundle from the main menu) gets torn down once FishNet's
        /// networked gameplay scenes finish loading - StartGame/HandleLoadEnd only know about
        /// FishNet global scenes, not whatever this (local, non-networked) manager has loaded, so
        /// that transition has to be told about it explicitly from there. Safe to call repeatedly,
        /// or when nothing is loaded - both are no-ops.
        /// </summary>
        public static void UnloadActiveBundle()
        {
            if (Instance == null || (ActiveBundle == null && LoadedScenes.Count == 0))
                return;

            if (Instance._loadCoroutine != null)
                Instance.StopCoroutine(Instance._loadCoroutine);

            Instance._loadCoroutine = Instance.StartCoroutine(Instance.UnloadActiveBundleRoutine());
        }

        private IEnumerator UnloadActiveBundleRoutine()
        {
            yield return UnloadLoadedScenes();

            ActiveBundle = null;
            _loadCoroutine = null;
        }

        private IEnumerator LoadBundleRoutine(ContentBundle bundle)
        {
            yield return UnloadLoadedScenes();

            yield return LoadSceneIntoBundle(bundle.BaseScene);
            Scene baseScene = SceneManager.GetSceneByName(bundle.BaseScene.SceneName);
            SceneManager.SetActiveScene(baseScene);

            foreach (var layer in bundle.Layers)
            {
                if (layer == null || !layer.IsAssigned)
                    continue;

                yield return LoadSceneIntoBundle(layer);
            }

            // Only spawns a GameMode if this bundle explicitly specifies one - unlike
            // SessionManager's networked flow, there is no default/fallback GameMode here,
            // since most bundles loaded through this path (main menu, lobby) are plain UI
            // with nothing to spawn.
            if (bundle.GameModePrefab != null)
            {
                GameModeBase gameMode = Instantiate(bundle.GameModePrefab);
                SceneManager.MoveGameObjectToScene(gameMode.gameObject, baseScene);
            }

            ActiveBundle = bundle;
            _loadCoroutine = null;
        }

        private IEnumerator LoadSceneIntoBundle(SceneReference scene)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(scene.SceneName, LoadSceneMode.Additive);
            yield return loadOperation;

            LoadedScenes.Add(SceneManager.GetSceneByName(scene.SceneName));
        }

        private static IEnumerator UnloadLoadedScenes()
        {
            foreach (var scene in LoadedScenes)
            {
                if (scene.IsValid())
                    yield return SceneManager.UnloadSceneAsync(scene);
            }

            LoadedScenes.Clear();
        }
    }
}
