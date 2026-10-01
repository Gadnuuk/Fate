using System.Collections;
using System.Collections.Generic;
using Fate.Systems.Content;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fate.Systems.Boot
{
    public class BootSequenceManager : MonoBehaviour
    {
        [SerializeField] private BootSequence bootSequence;

        private void Start()
        {
            StartCoroutine(RunBootSequence());
        }

        private IEnumerator RunBootSequence()
        {
            if (bootSequence == null)
            {
                Debug.LogError($"{nameof(BootSequenceManager)} has no {nameof(BootSequence)} assigned.", this);
                yield break;
            }

            BootSequenceEntry previousEntry = null;
            Scene previousScene = default;

            foreach (var entry in bootSequence.Entries)
            {
                if (entry == null || entry.Scene == null || !entry.Scene.IsAssigned)
                    continue;

                if (previousEntry != null && previousEntry.UnloadWhenNextSceneStarts)
                    SceneManager.UnloadSceneAsync(previousScene);

                SceneManager.LoadScene(entry.Scene.SceneName, LoadSceneMode.Additive);

                // LoadScene (sync) calls Awake/OnEnable immediately, but Start is
                // deferred to the next frame's Start phase. Waiting one frame here
                // guarantees Start has run on everything in the scene before we
                // move on to loading the next one.
                yield return null;

                var scene = SceneManager.GetSceneByName(entry.Scene.SceneName);
                yield return WaitForSceneReady(scene);

                previousEntry = entry;
                previousScene = scene;
            }

            if (bootSequence.ContentBundle != null)
                ContentSceneManager.LoadBundle(bootSequence.ContentBundle);
        }

        private IEnumerator WaitForSceneReady(Scene scene)
        {
            var components = new List<BootFinishedEventComponent>();
            foreach (var root in scene.GetRootGameObjects())
                components.AddRange(root.GetComponentsInChildren<BootFinishedEventComponent>(true));

            if (components.Count == 0)
                yield break;

            int remaining = components.Count;
            void OnComponentFinished() => remaining--;

            foreach (var component in components)
            {
                // Start may have already run (and fired Finished) before we got here,
                // so check HasFinished rather than only relying on the event.
                if (component.HasFinished)
                    remaining--;
                else
                    component.Finished += OnComponentFinished;
            }

            yield return new WaitUntil(() => remaining <= 0);

            foreach (var component in components)
                component.Finished -= OnComponentFinished;
        }
    }
}
