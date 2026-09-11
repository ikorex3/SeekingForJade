#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SeekingForJade.Editor
{
    public static class MaterialURPConverter
    {
        [MenuItem("SeekingForJade/Convert Imported Materials to URP")]
        public static void ConvertAllImportedMaterials()
        {
            string[] folders = new string[]
            {
                "Assets/BrokenVector",
                "Assets/SimpleNaturePack",
                "Assets/FantasyMedievalTown_LITE",
                "Assets/LowPolyMedievalPropsLite",
                "Assets/Blink"
            };

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpParticlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            if (urpLit == null)
            {
                Debug.LogError("[MaterialURPConverter] Universal Render Pipeline/Lit shader not found!");
                return;
            }

            int convertedCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:Material", folders);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string sName = mat.shader != null ? mat.shader.name : "";

                // Check if already URP
                if (sName.StartsWith("Universal Render Pipeline/"))
                {
                    continue;
                }

                // Check if it's a particle material
                bool isParticle = path.Contains("Particle") || path.Contains("FX") || sName.Contains("Particle");

                // Cache old properties
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
                Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.2f;

                if (isParticle)
                {
                    mat.shader = urpParticlesUnlit != null ? urpParticlesUnlit : urpLit;
                    if (mainTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mainColor);
                }
                else
                {
                    mat.shader = urpLit;
                    if (mainTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mainColor);
                    if (bumpMap != null && mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", bumpMap);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

                    if (emissionColor.maxColorComponent > 0.05f || emissionMap != null)
                    {
                        mat.EnableKeyword("_EMISSION");
                        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
                        if (emissionMap != null && mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", emissionMap);
                    }
                }

                EditorUtility.SetDirty(mat);
                convertedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green><b>[MaterialURPConverter]</b></color> Successfully converted {convertedCount} materials across store asset packs to URP!");
        }
    }
}
#endif
