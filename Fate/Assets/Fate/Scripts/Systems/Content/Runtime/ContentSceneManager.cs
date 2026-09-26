using System.Collections;
using System.Collections.Generic;
using Fate.Systems.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fate.Systems.Content
{
    /// <summary>
    /// Persistent singleton that loads and unloads content bundles (a base level scene
    /// plus its game-mode layer scenes). Call LoadBundle to swap out whatever content is
    /// currently loaded - the previous bundle's scenes are unloaded first.
    /// </summary>
    public class ContentSceneManager : MonoBehaviour
    {
        private static ContentSceneManager instance;
        private static readonly List<Scene> LoadedScenes = new();

        public static ContentBundle ActiveBundle { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            instance = null;
        }

        public static void LoadBundle(ContentBundle bundle)
        {
            if (instance == null)
            {
                Debug.LogError($"{nameof(ContentSceneManager)} has no active instance in the scene.");
                return;
            }

            if (bundle == null || bundle.BaseScene == null || !bundle.BaseScene.IsAssigned)
            {
                Debug.LogError($"{nameof(ContentSceneManager)} received an unassigned {nameof(ContentBundle)}.");
                return;
            }

            instance.StartCoroutine(instance.LoadBundleRoutine(bundle));
        }

        private IEnumerator LoadBundleRoutine(ContentBundle bundle)
        {
            yield return UnloadLoadedScenes();

            yield return LoadSceneIntoBundle(bundle.BaseScene);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(bundle.BaseScene.SceneName));

            foreach (var layer in bundle.Layers)
            {
                if (layer == null || !layer.IsAssigned)
                    continue;

                yield return LoadSceneIntoBundle(layer);
            }

            ActiveBundle = bundle;
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
