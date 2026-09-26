using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fate.Systems.Utilities
{
    [Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif
        [SerializeField] private string sceneName = string.Empty;
        [SerializeField] private string scenePath = string.Empty;

        public string SceneName => sceneName;
        public string ScenePath => scenePath;
        public bool IsAssigned => !string.IsNullOrEmpty(scenePath);

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (sceneAsset != null)
            {
                scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            }
            else
            {
                scenePath = string.Empty;
                sceneName = string.Empty;
            }
#endif
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
