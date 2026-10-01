using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TexasCashRegisterComplete.Editor
{
    public static class TexasCashRegisterCompleteRepair
    {
        const string UVMarker = "RegisterFrontLabelUVFixedV2";
        [MenuItem("Tools/Cash Register Complete/Repair Register")]
        public static void Repair() { RepairInternal(true); }
        public static void RepairAutomatic() { RepairInternal(false); }
        static void RepairInternal(bool showDialog)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Texas Shop", "Play 모드를 종료하고 실행해주세요.", "확인"); return;
            }
            string root = null;
            foreach (string scriptId in AssetDatabase.FindAssets("TexasCashRegisterCompleteRepair t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(scriptId);
                const string suffix = "/Editor/TexasCashRegisterCompleteRepair.cs";
                if (!path.EndsWith(suffix)) continue;
                string candidate = path.Substring(0, path.Length - suffix.Length);
                if (AssetDatabase.IsValidFolder(candidate + "/Meshes")) { root = candidate; break; }
            }
            if (root == null) { Debug.LogError("Keep TexasCashRegisterCompleteRepair.cs in the register asset's Editor folder."); return; }
            int repaired = 0, labels = 0, failed = 0;
            var paths = new List<string>();
            foreach (string id in AssetDatabase.FindAssets("t:Mesh", new[] { root + "/Meshes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                Mesh original = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (original == null || !IsRegisterMesh(original.name)) continue;
                Mesh rebuilt = null;
                try
                {
                    if (!original.isReadable || original.vertexCount == 0) throw new InvalidOperationException("Unreadable or empty mesh.");
                    var vertices = original.vertices; var normals = original.normals;
                    var uv = original.uv; var tangents = original.tangents;
                    var triangles = new List<int[]>();
                    for (int i = 0; i < original.subMeshCount; i++)
                    {
                        if (original.GetTopology(i) != MeshTopology.Triangles) throw new InvalidOperationException("Unexpected topology.");
                        int[] indexes = original.GetTriangles(i);
                        foreach (int index in indexes) if (index < 0 || index >= vertices.Length) throw new InvalidOperationException("Invalid triangle index.");
                        triangles.Add(indexes);
                    }
                    AssetImporter importer = AssetImporter.GetAtPath(path);
                    bool fixUV = (original.name == "Body_Register_Display" || original.name == "Drawer_Register_Brand") && importer != null && !importer.userData.Contains(UVMarker);
                    if (fixUV)
                    {
                        if (uv.Length != vertices.Length) throw new InvalidOperationException("Label UV data is missing.");
                        for (int i = 0; i < uv.Length; i++) uv[i].x = 1f - uv[i].x;
                        for (int i = 0; i < tangents.Length; i++) tangents[i] = -tangents[i];
                    }
                    rebuilt = new Mesh { name = original.name, indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    rebuilt.vertices = vertices;
                    if (uv.Length == vertices.Length) rebuilt.uv = uv;
                    if (normals.Length == vertices.Length) rebuilt.normals = normals;
                    rebuilt.subMeshCount = triangles.Count;
                    for (int i = 0; i < triangles.Count; i++) rebuilt.SetTriangles(triangles[i], i, true);
                    if (normals.Length != vertices.Length) rebuilt.RecalculateNormals();
                    if (tangents.Length == vertices.Length) rebuilt.tangents = tangents;
                    else if (uv.Length == vertices.Length) rebuilt.RecalculateTangents();
                    rebuilt.RecalculateBounds();
                    Undo.RegisterCompleteObjectUndo(original, "Repair Cash Register Mesh");
                    EditorUtility.CopySerialized(rebuilt, original);
                    EditorUtility.SetDirty(original);
                    AssetDatabase.SaveAssets();
                    if (fixUV)
                    {
                        importer.userData += (string.IsNullOrEmpty(importer.userData) ? "" : "\n") + UVMarker;
                        importer.SaveAndReimport(); labels++;
                    }
                    if (importer != null && !importer.userData.Contains("CashRegisterCompleteNativeV1"))
                    {
                        importer.userData += "\nCashRegisterCompleteNativeV1";
                        importer.SaveAndReimport();
                    }
                    paths.Add(path); repaired++;
                }
                catch (Exception ex) { failed++; Debug.LogError("Register repair failed: " + path + "\n" + ex); }
                finally { if (rebuilt != null) UnityEngine.Object.DestroyImmediate(rebuilt); }
            }
            AssetDatabase.SaveAssets();
            foreach (string path in paths) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var roots = new HashSet<GameObject>();
            foreach (MeshFilter filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                if (filter == null || EditorUtility.IsPersistent(filter) || !filter.gameObject.scene.IsValid() || filter.sharedMesh == null || !IsRegisterMesh(filter.sharedMesh.name)) continue;
                var register = filter.GetComponentInParent<TexasCashRegisterComplete.CashRegisterInteractable>();
                if (register != null) roots.Add(register.gameObject);
                SceneVisibilityManager.instance.EnablePicking(filter.gameObject, false);
            }
            foreach (GameObject go in roots) SceneVisibilityManager.instance.EnablePicking(go, true);
            SceneView.RepaintAll();
            if (showDialog) EditorUtility.DisplayDialog("Texas Shop", "메쉬 재구성: " + repaired + "개\n라벨 추가 수정: " + labels + "개\nScene 선택 허용: " + roots.Count + "개\n실패: " + failed + "개\n\n금전등록기를 다시 클릭해보세요.", "확인");
        }
        static bool IsRegisterMesh(string name)
        {
            return name.StartsWith("Body_Register_") || name.StartsWith("Drawer_Register_");
        }
    }
}
