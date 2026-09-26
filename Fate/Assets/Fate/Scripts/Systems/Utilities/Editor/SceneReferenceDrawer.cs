using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fate.Systems.Utilities.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        private const float LineSpacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return HasBuildSettingsWarning(property)
                ? EditorGUIUtility.singleLineHeight * 2 + LineSpacing
                : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var sceneAssetProp = property.FindPropertyRelative("sceneAsset");

            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            var newAsset = EditorGUI.ObjectField(fieldRect, label, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetProp.objectReferenceValue = newAsset;
            }

            var sceneAsset = sceneAssetProp.objectReferenceValue as SceneAsset;
            if (sceneAsset != null && !IsInBuildSettings(sceneAsset))
            {
                var warningRect = new Rect(position.x, fieldRect.yMax + LineSpacing, position.width - 90, EditorGUIUtility.singleLineHeight);
                var buttonRect = new Rect(warningRect.xMax, warningRect.y, 90, EditorGUIUtility.singleLineHeight);
                EditorGUI.HelpBox(warningRect, "Not in Build Settings", MessageType.Warning);
                if (GUI.Button(buttonRect, "Add"))
                {
                    AddSceneToBuildSettings(sceneAsset);
                }
            }

            EditorGUI.EndProperty();
        }

        private static bool HasBuildSettingsWarning(SerializedProperty property)
        {
            var sceneAsset = property.FindPropertyRelative("sceneAsset").objectReferenceValue as SceneAsset;
            return sceneAsset != null && !IsInBuildSettings(sceneAsset);
        }

        private static bool IsInBuildSettings(SceneAsset sceneAsset)
        {
            var path = AssetDatabase.GetAssetPath(sceneAsset);
            return EditorBuildSettings.scenes.Any(s => s.path == path);
        }

        private static void AddSceneToBuildSettings(SceneAsset sceneAsset)
        {
            var path = AssetDatabase.GetAssetPath(sceneAsset);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path))
                return;

            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
