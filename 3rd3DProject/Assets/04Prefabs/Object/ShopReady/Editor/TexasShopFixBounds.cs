using UnityEngine;
using UnityEditor;

namespace TexasShopReady.Editor
{
    public static class TexasShopFixBounds
    {
        [MenuItem("Tools/Texas Shop Ready/Fix Mesh Bounds")]
        public static void Fix()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Texas Shop", "Play 모드를 종료하고 실행해주세요.", "확인"); return;
            }
            int count = 0;
            foreach (string id in AssetDatabase.FindAssets("t:Mesh"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                if (!path.EndsWith(".asset")) continue;
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null || !IsShopMesh(mesh.name)) continue;
                if (!mesh.isReadable || mesh.vertexCount == 0) { Debug.LogWarning("Cannot repair unreadable mesh: " + path); continue; }
                Undo.RecordObject(mesh, "Repair Shop Mesh Bounds");
                mesh.RecalculateBounds();
                // SetSubMesh recalculates per-submesh bounds and vertex range using the index buffer.
                for (int i = 0; i < mesh.subMeshCount; i++) mesh.SetSubMesh(i, mesh.GetSubMesh(i));
                EditorUtility.SetDirty(mesh);
                count++;
            }
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
            EditorUtility.DisplayDialog("Texas Shop", count + "개의 메쉬 표시·선택 범위를 다시 계산했습니다.", "확인");
        }
        static bool IsShopMesh(string name)
        {
            return name.StartsWith("CashTray_TSP_") || name.StartsWith("CashBundle_TSP_") || name.StartsWith("GoldEmmental_TSP_") || name.StartsWith("ReserveWineBottle_TSP_") || name.StartsWith("ReserveWineBottle_Premium_") || name.StartsWith("PowerBankBox_TSP_") || name.StartsWith("PowerBankBox_Premium_") || name.StartsWith("Body_Register_") || name.StartsWith("Drawer_Register_");
        }
    }
}
