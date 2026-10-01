using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TexasJawTrap.Editor
{
    [InitializeOnLoad]
    public static class JawTrapNativeSetup
    {
        const string Marker = "TexasJawTrapNativeV1";
        static readonly double ReadyAfter;
        static JawTrapNativeSetup()
        {
            ReadyAfter = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += SetupWhenReady;
        }
        static void SetupWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < ReadyAfter) return;
            EditorApplication.update -= SetupWhenReady;
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Rebuild(false);
        }
        [MenuItem("Tools/Texas Jaw Trap/Rebuild Native Meshes")]
        public static void RebuildAll() { Rebuild(true); }
        static void Rebuild(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string root = null;
            foreach (string id in AssetDatabase.FindAssets("JawTrapNativeSetup t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                const string suffix = "/Editor/JawTrapNativeSetup.cs";
                if (path.EndsWith(suffix)) { root = path.Substring(0, path.Length - suffix.Length); break; }
            }
            if (root == null || !AssetDatabase.IsValidFolder(root + "/Meshes")) return;
            foreach (string id in AssetDatabase.FindAssets("t:Mesh", new[] { root + "/Meshes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                AssetImporter importer = AssetImporter.GetAtPath(path);
                if (!force && importer != null && importer.userData.Contains(Marker)) continue;
                Mesh original = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                Mesh fresh = null;
                try
                {
                    if (original == null || !original.isReadable || original.vertexCount == 0) throw new InvalidOperationException("Mesh is empty or unreadable.");
                    Vector3[] vertices = original.vertices;
                    Vector3[] normals = original.normals;
                    Vector2[] uv = original.uv;
                    Vector4[] tangents = original.tangents;
                    var triangles = new List<int[]>();
                    for (int i = 0; i < original.subMeshCount; i++) triangles.Add(original.GetTriangles(i));
                    fresh = new Mesh { name = original.name, indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    fresh.vertices = vertices;
                    if (uv.Length == vertices.Length) fresh.uv = uv;
                    if (normals.Length == vertices.Length) fresh.normals = normals;
                    fresh.subMeshCount = triangles.Count;
                    for (int i = 0; i < triangles.Count; i++) fresh.SetTriangles(triangles[i], i, true);
                    if (normals.Length != vertices.Length) fresh.RecalculateNormals();
                    if (tangents.Length == vertices.Length) fresh.tangents = tangents;
                    else if (uv.Length == vertices.Length) fresh.RecalculateTangents();
                    fresh.RecalculateBounds();
                    EditorUtility.CopySerialized(fresh, original);
                    EditorUtility.SetDirty(original);
                    AssetDatabase.SaveAssets();
                    if (importer != null && !importer.userData.Contains(Marker))
                    {
                        importer.userData += "\n" + Marker;
                        importer.SaveAndReimport();
                    }
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                catch (Exception ex) { Debug.LogError("TexasJawTrap setup failed: " + path + "\n" + ex); }
                finally { if (fresh != null) UnityEngine.Object.DestroyImmediate(fresh); }
            }
            TexasJawTrapMaterials.Setup();
            foreach (var trap in Resources.FindObjectsOfTypeAll<TexasJawTrap.ConsumableJawTrap>())
                if (!EditorUtility.IsPersistent(trap) && trap.gameObject.scene.IsValid()) SceneVisibilityManager.instance.EnablePicking(trap.gameObject, true);
            SceneView.RepaintAll();
        }
    }
}
