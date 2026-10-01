using UnityEngine;

namespace TexasShopReady
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu("Texas Shop/Select Whole Prop")]
    public class TexasShopSelectRoot : MonoBehaviour
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Texas Shop Ready/Select Props as Whole Objects")]
        static void Apply()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorUtility.DisplayDialog("Texas Shop", "Play 모드를 종료하고 실행해주세요.", "확인"); return;
            }
            int prefabs = 0, scenes = 0;
            foreach (string id in UnityEditor.AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(id);
                GameObject asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || !IsOurPropRoot(asset) || asset.GetComponent<TexasShopSelectRoot>() != null) continue;
                GameObject root = null;
                try
                {
                    root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
                    if (root.GetComponent<TexasShopSelectRoot>() == null) root.AddComponent<TexasShopSelectRoot>();
                    bool success;
                    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path, out success);
                    if (success) prefabs++;
                }
                finally { if (root != null) UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || UnityEditor.EditorUtility.IsPersistent(go) || !go.scene.IsValid() || !IsOurPropRoot(go)) continue;
                if (go.GetComponent<TexasShopSelectRoot>() != null) continue;
                UnityEditor.Undo.AddComponent<TexasShopSelectRoot>(go);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                scenes++;
            }
            // Promote the currently selected part to its newly marked parent.
            GameObject selected = UnityEditor.Selection.activeGameObject;
            if (selected != null)
            {
                TexasShopSelectRoot root = selected.GetComponentInParent<TexasShopSelectRoot>();
                if (root != null) UnityEditor.Selection.activeGameObject = root.gameObject;
            }
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.SceneView.RepaintAll();
            UnityEditor.EditorUtility.DisplayDialog("Texas Shop", "프리팹 적용: " + prefabs + "개\n추가 Scene 적용: " + scenes + "개\n\n소품 전체 선택을 설정했습니다. Scene을 저장해주세요.", "확인");
        }
        static bool IsOurPropRoot(GameObject go)
        {
            string n = go.name;
            if (!(n.StartsWith("CashTray") || n.StartsWith("CashBundle") || n.StartsWith("GoldEmmental") || n.StartsWith("ReserveWineBottle") || n.StartsWith("PowerBankBox") || n.StartsWith("CashRegister_Interactive"))) return false;
            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                string mesh = filter.sharedMesh.name;
                if (mesh.StartsWith("CashTray_TSP_") || mesh.StartsWith("CashBundle_TSP_") || mesh.StartsWith("GoldEmmental_TSP_") || mesh.StartsWith("ReserveWineBottle_TSP_") || mesh.StartsWith("ReserveWineBottle_Premium_") || mesh.StartsWith("PowerBankBox_TSP_") || mesh.StartsWith("PowerBankBox_Premium_") || mesh.StartsWith("Body_Register_") || mesh.StartsWith("Drawer_Register_")) return true;
            }
            return false;
        }
#endif
    }
}
