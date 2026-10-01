using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace TexasCashRegisterComplete.Editor
{
    // Meshes and prefabs are already present. Only select the project's material shader.
    [InitializeOnLoad]
    public static class TexasCashRegisterCompleteMaterials
    {
        static bool pending = true;
        static TexasCashRegisterCompleteMaterials() { EditorApplication.update += OnUpdate; }
        static void OnUpdate()
        {
            if (!pending || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            pending = false;
            EditorApplication.update -= OnUpdate;
            Setup();
        }
        [MenuItem("Tools/Cash Register Complete/Match Project Materials")]
        public static void Setup()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            string type = pipeline == null ? "" : pipeline.GetType().Name;
            string shaderName = type.Contains("HDRender") ? "HDRP/Lit" : type.Length > 0 ? "Universal Render Pipeline/Lit" : "Standard";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogWarning("TexasCashRegisterComplete: shader unavailable: " + shaderName); return; }
            // Resolve this script's location instead of assuming a fixed Assets folder.
            string root = null;
            foreach (string scriptId in AssetDatabase.FindAssets("TexasCashRegisterCompleteMaterials t:MonoScript"))
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptId);
                if (!scriptPath.EndsWith("/Editor/TexasCashRegisterCompleteMaterials.cs")) continue;
                string candidate = scriptPath.Substring(0, scriptPath.Length - "/Editor/TexasCashRegisterCompleteMaterials.cs".Length);
                if (AssetDatabase.IsValidFolder(candidate + "/Materials")) { root = candidate; break; }
            }
            if (root == null) { Debug.LogWarning("TexasCashRegisterComplete: keep this script in the asset folder's Editor directory, beside Materials."); return; }
            foreach (string id in AssetDatabase.FindAssets("t:Material", new[] { root + "/Materials" }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
                if (mat == null || mat.shader == shader) continue;
                bool standard = mat.shader != null && mat.shader.name == "Standard";
                Color color = mat.HasProperty(standard ? "_Color" : "_BaseColor") ? mat.GetColor(standard ? "_Color" : "_BaseColor") : Color.white;
                Texture tex = mat.HasProperty(standard ? "_MainTex" : "_BaseMap") ? mat.GetTexture(standard ? "_MainTex" : "_BaseMap") : null;
                Texture normal = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : mat.HasProperty("_NormalMap") ? mat.GetTexture("_NormalMap") : null;
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0;
                float smooth = mat.HasProperty(standard ? "_Glossiness" : "_Smoothness") ? mat.GetFloat(standard ? "_Glossiness" : "_Smoothness") : .5f;
                float bump = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : .4f;
                bool transparent = mat.name == "TSP_Bottle_001" || mat.name == "Premium_Wine";
                mat.shader = shader; mat.shaderKeywords = new string[0];
                foreach (string key in new[] { "_Color", "_BaseColor" }) if (mat.HasProperty(key)) mat.SetColor(key, color);
                foreach (string key in new[] { "_MainTex", "_BaseMap", "_BaseColorMap" }) if (mat.HasProperty(key)) mat.SetTexture(key, tex);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                foreach (string key in new[] { "_Glossiness", "_Smoothness" }) if (mat.HasProperty(key)) mat.SetFloat(key, smooth);
                if (normal != null)
                {
                    foreach (string key in new[] { "_BumpMap", "_NormalMap" }) if (mat.HasProperty(key)) mat.SetTexture(key, normal);
                    mat.EnableKeyword("_NORMALMAP"); mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                    foreach (string key in new[] { "_BumpScale", "_NormalScale" }) if (mat.HasProperty(key)) mat.SetFloat(key, bump);
                }
                mat.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
                foreach (string key in new[] { "_Surface", "_SurfaceType" }) if (mat.HasProperty(key)) mat.SetFloat(key, transparent ? 1 : 0);
                if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", transparent ? 2 : 0);
                if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", transparent ? (int)BlendMode.SrcAlpha : (int)BlendMode.One);
                if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", transparent ? (int)BlendMode.OneMinusSrcAlpha : (int)BlendMode.Zero);
                foreach (string key in new[] { "_ZWrite", "_TransparentZWrite" }) if (mat.HasProperty(key)) mat.SetFloat(key, transparent ? 0 : 1);
                if (transparent) mat.EnableKeyword(shaderName == "Standard" ? "_ALPHABLEND_ON" : "_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = transparent ? (mat.name == "TSP_Bottle_001" ? 3100 : 3000) : -1;
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
