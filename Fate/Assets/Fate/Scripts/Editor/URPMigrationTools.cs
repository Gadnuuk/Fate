using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fate.EditorTools
{
    // One-off migration helpers for finishing the Built-in -> URP conversion.
    // Safe to delete once the project is fully on URP.
    public static class URPMigrationTools
    {
        [MenuItem("Tools/Fate/URP Migration/Convert Legacy Particle Shaders (Additive + AlphaBlended Premultiply)")]
        public static void ConvertLegacyParticleShaders()
        {
            var upgraders = new List<MaterialUpgrader>();

            var additive = new MaterialUpgrader();
            additive.RenameShader("Particles/Additive", "Universal Render Pipeline/Particles/Unlit", FinalizeAdditive);
            additive.RenameTexture("_MainTex", "_BaseMap");
            additive.RenameColor("_Color", "_BaseColor");
            upgraders.Add(additive);

            var premultiply = new MaterialUpgrader();
            premultiply.RenameShader("Particles/Alpha Blended Premultiply", "Universal Render Pipeline/Particles/Unlit", FinalizePremultiply);
            premultiply.RenameTexture("_MainTex", "_BaseMap");
            premultiply.RenameColor("_Color", "_BaseColor");
            upgraders.Add(premultiply);

            var shadersToIgnore = new HashSet<string>();
            MaterialUpgrader.UpgradeProjectFolder(upgraders, shadersToIgnore, "Convert legacy particle shaders to URP",
                MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound);

            Debug.Log("Fate URP migration: legacy particle shader conversion finished. Check the Console above for any materials that had no matching upgrader.");
        }

        static void FinalizeAdditive(Material mat)
        {
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_Surface", (float)UpgradeSurfaceType.Transparent);
            mat.SetFloat("_Blend", (float)UpgradeBlendMode.Additive);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static void FinalizePremultiply(Material mat)
        {
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_Surface", (float)UpgradeSurfaceType.Transparent);
            mat.SetFloat("_Blend", (float)UpgradeBlendMode.Premultiply);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        [MenuItem("Tools/Fate/URP Migration/Wire Up URP Pipeline Assets")]
        public static void WireUpPipelineAssets()
        {
            var pcAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            var mobileAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");

            if (pcAsset == null || mobileAsset == null)
            {
                Debug.LogError("Fate URP migration: could not find Assets/Settings/PC_RPAsset.asset or Mobile_RPAsset.asset. Aborting.");
                return;
            }

            GraphicsSettings.defaultRenderPipeline = pcAsset;

            var tierAssets = new[] { mobileAsset, mobileAsset, mobileAsset, pcAsset, pcAsset, pcAsset };
            int levelCount = QualitySettings.count;
            for (int i = 0; i < levelCount && i < tierAssets.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = tierAssets[i];
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Fate URP migration: assigned default pipeline ({pcAsset.name}) and wired all {levelCount} quality tiers " +
                      "(Very Low/Low/Medium -> Mobile_RPAsset, High/Very High/Ultra -> PC_RPAsset).");
        }
    }
}
