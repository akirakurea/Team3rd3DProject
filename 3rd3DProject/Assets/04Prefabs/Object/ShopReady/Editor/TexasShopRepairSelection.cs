using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace TexasShopReady.Editor
{
    public static class TexasShopRepairSelection
    {
        [MenuItem("Tools/Texas Shop Ready/Repair Scene Selection")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Texas Shop", "Play 모드를 종료하고 실행해주세요.", "확인"); return;
            }
            int repaired = 0, failed = 0;
            var paths = new List<string>();
            foreach (string id in AssetDatabase.FindAssets("t:Mesh"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                if (!path.EndsWith(".asset")) continue;
                Mesh original = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (original == null || !IsShopMesh(original.name)) continue;
                Mesh rebuilt = null;
                try
                {
                    if (!original.isReadable || original.vertexCount == 0) throw new InvalidOperationException("Mesh is unreadable or empty.");
                    var vertices = original.vertices;
                    var normals = original.normals;
                    var uv = original.uv;
                    var tangents = original.tangents;
                    var indices = new List<int[]>();
                    for (int i = 0; i < original.subMeshCount; i++)
                    {
                        if (original.GetTopology(i) != MeshTopology.Triangles) throw new InvalidOperationException("Unexpected mesh topology.");
                        int[] triangle = original.GetTriangles(i);
                        foreach (int index in triangle) if (index < 0 || index >= vertices.Length) throw new InvalidOperationException("Triangle index is invalid.");
                        indices.Add(triangle);
                    }
                    // Initialize a fresh native Mesh, preserving the existing asset identity.
                    rebuilt = new Mesh { name = original.name, indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    rebuilt.vertices = vertices;
                    if (uv.Length == vertices.Length) rebuilt.uv = uv;
                    if (normals.Length == vertices.Length) rebuilt.normals = normals;
                    rebuilt.subMeshCount = indices.Count;
                    for (int i = 0; i < indices.Count; i++) rebuilt.SetTriangles(indices[i], i, true);
                    if (normals.Length != vertices.Length) rebuilt.RecalculateNormals();
                    if (tangents.Length == vertices.Length) rebuilt.tangents = tangents;
                    else if (uv.Length == vertices.Length) rebuilt.RecalculateTangents();
                    rebuilt.RecalculateBounds();
                    Undo.RegisterCompleteObjectUndo(original, "Repair Shop Mesh Selection");
                    Vector3 before = original.bounds.size;
                    EditorUtility.CopySerialized(rebuilt, original);
                    EditorUtility.SetDirty(original);
                    paths.Add(path); repaired++;
                    Debug.Log("Texas Shop repaired " + original.name + ": bounds " + before + " -> " + original.bounds.size, original);
                }
                catch (Exception ex) { failed++; Debug.LogError("Texas Shop failed: " + path + "\n" + ex); }
                finally { if (rebuilt != null) UnityEngine.Object.DestroyImmediate(rebuilt); }
            }
            AssetDatabase.SaveAssets();
            foreach (string path in paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var roots = new HashSet<GameObject>();
            foreach (MeshFilter filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                if (filter == null || EditorUtility.IsPersistent(filter) || !filter.gameObject.scene.IsValid()) continue;
                if (filter.sharedMesh == null || !IsShopMesh(filter.sharedMesh.name)) continue;
                GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(filter.gameObject);
                if (root == null) root = filter.gameObject;
                roots.Add(root);
                SceneVisibilityManager.instance.EnablePicking(filter.gameObject, false);
            }
            foreach (GameObject root in roots) SceneVisibilityManager.instance.EnablePicking(root, true);
            SceneView.RepaintAll();
            EditorUtility.DisplayDialog("Texas Shop", "메쉬 재구성: " + repaired + "개\n선택 허용: " + roots.Count + "개 소품\n실패: " + failed + "개\n\nScene 창에서 치즈를 다시 클릭해보세요.", "확인");
        }
        static bool IsShopMesh(string name)
        {
            return name.StartsWith("CashTray_TSP_") || name.StartsWith("CashBundle_TSP_") || name.StartsWith("GoldEmmental_TSP_") || name.StartsWith("ReserveWineBottle_TSP_") || name.StartsWith("ReserveWineBottle_Premium_") || name.StartsWith("PowerBankBox_TSP_") || name.StartsWith("PowerBankBox_Premium_") || name.StartsWith("Body_Register_") || name.StartsWith("Drawer_Register_");
        }
    }
}
