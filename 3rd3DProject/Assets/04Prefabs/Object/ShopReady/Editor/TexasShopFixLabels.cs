using UnityEngine;
using UnityEditor;

namespace TexasShopReady.Editor
{
    public static class TexasShopFixLabels
    {
        const string Marker = "TexasShopLabelUVFixedV1";
        [MenuItem("Tools/Texas Shop Ready/Fix Mirrored Labels")]
        public static void Fix()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Texas Shop", "Play 모드를 종료한 후 실행해주세요.", "확인");
                return;
            }
            int count = 0;
            foreach (string id in AssetDatabase.FindAssets("t:Mesh"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                if (!path.EndsWith(".asset")) continue;
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null || !IsLabel(mesh.name)) continue;
                AssetImporter importer = AssetImporter.GetAtPath(path);
                if (importer == null || importer.userData.Contains(Marker)) continue;
                Vector2[] uv = mesh.uv;
                if (uv.Length == 0 || uv.Length != mesh.vertexCount) continue;
                Undo.RecordObject(mesh, "Fix Shop Label UV");
                for (int i = 0; i < uv.Length; i++) uv[i].x = 1f - uv[i].x;
                mesh.uv = uv;
                Vector4[] tangents = mesh.tangents;
                if (tangents.Length == mesh.vertexCount)
                {
                    for (int i = 0; i < tangents.Length; i++)
                    {
                        tangents[i].x = -tangents[i].x;
                        tangents[i].y = -tangents[i].y;
                        tangents[i].z = -tangents[i].z;
                        tangents[i].w = -tangents[i].w;
                    }
                    mesh.tangents = tangents;
                }
                EditorUtility.SetDirty(mesh);
                importer.userData += (string.IsNullOrEmpty(importer.userData) ? "" : "\n") + Marker;
                AssetDatabase.SaveAssets();
                importer.SaveAndReimport();
                count++;
            }
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
            EditorUtility.DisplayDialog("Texas Shop", count > 0 ? count + "개의 라벨 좌우 반전을 수정했습니다." : "추가로 수정할 라벨이 없습니다. 이미 수정된 파일은 건너뜁니다.", "확인");
        }
        static bool IsLabel(string name)
        {
            return name.Contains("_TSP_CashLabel_") || name.Contains("_TSP_WineLabel_") || name.Contains("_TSP_CheeseLabel_") || name.Contains("_TSP_PowerLabel_");
        }
    }
}
