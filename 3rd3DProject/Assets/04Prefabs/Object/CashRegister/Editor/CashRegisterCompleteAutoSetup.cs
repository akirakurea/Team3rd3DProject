using UnityEditor;
using UnityEngine;

namespace TexasCashRegisterComplete.Editor
{
    [InitializeOnLoad]
    public static class CashRegisterCompleteAutoSetup
    {
        static readonly double ReadyAfter;
        static CashRegisterCompleteAutoSetup()
        {
            ReadyAfter = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Setup;
        }
        static void Setup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < ReadyAfter) return;
            EditorApplication.update -= Setup;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string root = null;
            foreach (string id in AssetDatabase.FindAssets("CashRegisterCompleteAutoSetup t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                const string suffix = "/Editor/CashRegisterCompleteAutoSetup.cs";
                if (path.EndsWith(suffix)) { root = path.Substring(0, path.Length - suffix.Length); break; }
            }
            if (root == null || !AssetDatabase.IsValidFolder(root + "/Meshes")) return;
            bool needsRepair = false;
            foreach (string id in AssetDatabase.FindAssets("t:Mesh", new[] { root + "/Meshes" }))
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(id));
                if (importer != null && !importer.userData.Contains("CashRegisterCompleteNativeV1")) { needsRepair = true; break; }
            }
            if (needsRepair) TexasCashRegisterCompleteRepair.RepairAutomatic();
            TexasCashRegisterCompleteMaterials.Setup();
        }
    }
}
