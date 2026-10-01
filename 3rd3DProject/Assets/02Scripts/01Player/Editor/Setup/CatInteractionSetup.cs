using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>PlayerTestScene에만 적용하는 명시적 설치 도구. 자동 실행하지 않습니다.</summary>
public static class CatInteractionSetup
{
    const string Scripts = "Assets/02Scripts/01Player";
    const string AssetsRoot = "Assets/03Sprites/Player/Interaction";
    static void Guard()
    {
        if (SceneManager.GetActiveScene().path != "Assets/01Scenes/PlayerTestScene.unity") throw new InvalidOperationException("PlayerTestScene만 작업할 수 있습니다.");
        if (EditorApplication.isPlaying) throw new InvalidOperationException("편집 모드에서 실행하세요.");
    }
    public static void Inspect()
    {
        var s = SceneManager.GetActiveScene(); var b = new StringBuilder(s.path + "\n");
        foreach (var root in s.GetRootGameObjects()) b.AppendLine("ROOT " + root.name + " " + root.transform.position);
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (r.name.StartsWith("Shelf_Deck") || r.name.StartsWith("Shelf_Spine")) b.AppendLine(r.name + " " + r.bounds);
        foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) b.AppendLine("CANVAS " + c.name + " " + c.renderMode);
        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) b.AppendLine("CAMERA " + c.name + " " + c.transform.position + " " + c.transform.eulerAngles);
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/CatInteractionSetup.txt", b.ToString());
    }
    [MenuItem("Tools/Cat Player/요청 스크립트 폴더 정리")]
    public static void OrganizeScripts()
    {
        Guard();
        Move("CatPlayerMotor.cs", "Core"); Move("CatPlayerInput.cs", "Input");
        Move("CatPlayerLocomotion.cs", "Movement"); Move("CatStepSettings.cs", "Movement"); Move("CatStepSolver.cs", "Movement");
        Move("CatPlayerAnimation.cs", "Animation"); Move("CatCinemachineCursor.cs", "Camera"); Move("CatCameraLook.inputactions", "Camera");
        Move("Editor/CatPlayerValidation.cs", "Editor/Validation"); Move("Editor/CatPlayerValidationCases.cs", "Editor/Validation");
        // MoveAsset 자체가 이동을 저장합니다. 다른 에셋의 미저장 변경은 저장하지 않습니다.
    }
    static void Move(string source, string folder)
    {
        string old = Scripts + "/" + source, dest = Scripts + "/" + folder + "/" + Path.GetFileName(source);
        if (!File.Exists(old)) return;
        Folder(Scripts + "/" + folder);
        string error = AssetDatabase.MoveAsset(old, dest);
        if (!string.IsNullOrEmpty(error)) throw new IOException(error);
    }
    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    static Material Material(string name, string shader, Color color, float extrusion = 0, float cull = 2)
    {
        string path = AssetsRoot + "/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
        var s = Shader.Find(shader); if (s == null) throw new InvalidOperationException("Shader missing: " + shader);
        var mat = new Material(s) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Extrusion")) mat.SetFloat("_Extrusion", extrusion);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", cull);
        AssetDatabase.CreateAsset(mat, path); return mat;
    }
    [MenuItem("Tools/Cat Player/상호작용 샘플 설치")]
    public static void Configure()
    {
        Guard();
        if (GameObject.Find("Cat_Interaction_Items") != null) throw new InvalidOperationException("이미 설치되어 있습니다. 기존 항목을 덮어쓰지 않습니다.");
        var player = GameObject.Find("Cat_Player"); var camera = Camera.main;
        if (player == null || camera == null) throw new InvalidOperationException("Player / Main Camera missing");
        if (player.scene != SceneManager.GetActiveScene() || camera.gameObject.scene != SceneManager.GetActiveScene()) throw new InvalidOperationException("다른 씬의 플레이어나 카메라는 수정할 수 없습니다.");
        Folder(AssetsRoot + "/Materials"); Folder(AssetsRoot + "/Prefabs");
        var fill = Material("Hover_Fill", "Cat Player/Interaction Overlay", new Color(1, .91f, .25f, .19f));
        var edge = Material("Hover_Edge", "Cat Player/Interaction Overlay", new Color(1, .68f, .015f, .9f), .006f, 1);
        var halo = Material("Hover_Halo", "Cat Player/Interaction Overlay", new Color(1, .78f, .035f, .15f), .014f, 1);
        var cylinderMat = Material("Cylinder_Blue", "Universal Render Pipeline/Lit", new Color(.2f, .52f, .64f));
        var cubeMat = Material("Cube_Cream", "Universal Render Pipeline/Lit", new Color(.78f, .61f, .36f));
        var cursorMat = Material("Cursor_White", "Cat Player/Cursor Sphere", new Color(1, 1, 1, .5f));
        var cylinder = MakePrefab("CarryCylinder", PrimitiveType.Cylinder, CatItemKind.CarryOnly, new Vector3(.22f, .16f, .22f), cylinderMat);
        var cube = MakePrefab("InventoryCube", PrimitiveType.Cube, CatItemKind.InventoryPickup, new Vector3(.3f, .095f, .24f), cubeMat);
        var root = new GameObject("Cat_Interaction_Items"); Undo.RegisterCreatedObjectUndo(root, "상호작용 아이템 설치");
        // 현재 매대의 앞쪽 두 단. 원본 맵/프리팹은 수정하지 않습니다.
        for (int i = 0; i < 4; i++) Place(cylinder, root.transform, "Cylinder_" + (i + 1), new Vector3(3.4f + .7f * i, 1.006f, -13.57f));
        for (int i = 0; i < 2; i++) Place(cube, root.transform, "Cube_" + (i + 1), new Vector3(4.1f + 1.1f * i, .169f, -12.35f));
        var presenter = Undo.AddComponent<CatInventoryPickupPresenter>(player);
        var control = Undo.AddComponent<CatInteractionController>(player);
        control.view = camera; control.pickup = presenter;
        control.highlightFill = fill; control.highlightEdge = edge; control.highlightHalo = halo;
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "Cat_CenterCursorSphere";
        UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(camera.transform, false); sphere.transform.localPosition = new Vector3(0, 0, .2f); sphere.transform.localScale = Vector3.one * .004f;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = cursorMat;
        sphere.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sphere.GetComponent<MeshRenderer>().receiveShadows = false;
        var cursor = Undo.AddComponent<CatCenterCursor>(camera.gameObject); cursor.view = camera; cursor.sphere = sphere.transform;
        Undo.RegisterCreatedObjectUndo(sphere, "중앙 구체 커서 설치");
        EditorUtility.SetDirty(player); EditorUtility.SetDirty(camera.gameObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        foreach (string guid in AssetDatabase.FindAssets("", new[] { AssetsRoot }))
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); Inspect();
    }
    static GameObject MakePrefab(string name, PrimitiveType shape, CatItemKind kind, Vector3 scale, Material mat)
    {
        string path = AssetsRoot + "/Prefabs/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
        var root = new GameObject(name); var visual = GameObject.CreatePrimitive(shape);
        visual.name = "Visual"; visual.transform.SetParent(root.transform, false); visual.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>()); visual.GetComponent<Renderer>().sharedMaterial = mat;
        var item = root.AddComponent<CatInteractionItem>(); item.visualRoot = visual.transform; item.itemId = name; item.kind = kind;
        item.Body.isKinematic = true; item.Body.useGravity = false; item.RefreshVisual();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
    }
    static void Place(GameObject prefab, Transform parent, string name, Vector3 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = name; go.transform.SetParent(parent); go.transform.position = position;
        go.GetComponent<CatInteractionItem>().itemId = name;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<CatInteractionItem>());
        Undo.RegisterCreatedObjectUndo(go, "샘플 배치");
    }
}
