using UnityEngine;

namespace Fate.Systems.Utilities
{
    /// <summary>
    /// Base class for a persistent, cross-scene MonoBehaviour singleton (the pattern
    /// <see cref="Fate.Systems.Content.ContentSceneManager"/> and
    /// <see cref="Fate.Systems.Player.ProfileManager"/> both follow).
    ///
    /// Preferred setup is to place exactly one instance on a GameObject that's part of the boot
    /// sequence, so it exists before anything asks for it - <see cref="Instance"/> finds and
    /// adopts that placed instance on first access. Nothing placed one yet? <see cref="Instance"/>
    /// lazily creates one on its own dedicated GameObject instead, so call sites never have to
    /// null-check or spawn a manager themselves. Either way, exactly one instance ever survives:
    /// a second instance appearing anywhere (e.g. a scene reload) destroys itself in
    /// <see cref="Awake"/> rather than taking over.
    /// </summary>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindFirstObjectByType<T>();
                if (instance == null)
                    instance = new GameObject(typeof(T).Name).AddComponent<T>();

                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = (T)this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
