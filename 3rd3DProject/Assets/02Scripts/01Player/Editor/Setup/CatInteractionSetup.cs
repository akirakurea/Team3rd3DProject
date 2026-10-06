using System;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>허용 씬에서 기존 아이템·재질을 사용해 상호작용 연결을 복구합니다. 자동 실행하지 않습니다.</summary>
public static class CatInteractionSetup
{
    public static void Inspect()
    {
        var s = CatPlayerEditorScope.RequireScene(); var b = new StringBuilder(s.path + "\n");
        foreach (var root in s.GetRootGameObjects()) b.AppendLine("ROOT " + root.name + " " + root.transform.position);
        foreach (var r in CatPlayerEditorScope.FindAll<MeshRenderer>())
            if (r.name.StartsWith("Shelf_Deck") || r.name.StartsWith("Shelf_Spine")) b.AppendLine(r.name + " " + r.bounds);
        foreach (var c in CatPlayerEditorScope.FindAll<Canvas>()) b.AppendLine("CANVAS " + c.name + " " + c.renderMode);
        foreach (var c in CatPlayerEditorScope.FindAll<Camera>()) b.AppendLine("CAMERA " + c.name + " " + c.transform.position + " " + c.transform.eulerAngles);
        Debug.Log(b.ToString());
    }
    [MenuItem("Tools/Cat Player/상호작용 연결 확인 및 설치")]
    public static void Configure()
    {
        var scene = CatPlayerEditorScope.RequireScene(editMode: true);
        CatPlayerEditorScope.RequireValidationIdle();
        var motor = CatPlayerEditorScope.FindSingle<CatPlayerMotor>();
        var player = motor.gameObject;
        var camera = CatPlayerEditorScope.OutputCamera(motor);
        var control = player.GetComponent<CatInteractionController>();
        var presenter = player.GetComponent<CatInventoryPickupPresenter>();
        var cursor = camera.GetComponent<CatCenterCursor>();
        var root = CatPlayerEditorScope.FindObject("Cat_Interaction_Items");
        var sphere = cursor && CatPlayerEditorScope.Owns(cursor.sphere) ? cursor.sphere.gameObject :
            CatPlayerEditorScope.FindObject("Cat_CenterCursorSphere");
        // 연결이 끊긴 항목만 기존 GUID로 읽습니다. 새 에셋·meta는 만들지 않습니다.
        var fill = control && control.highlightFill ? control.highlightFill :
            CatPlayerEditorScope.ExistingPlayerAsset<Material>("a74a8c465545c974285be8a02102dc55", "Hover_Fill");
        var edge = control && control.highlightEdge ? control.highlightEdge :
            CatPlayerEditorScope.ExistingPlayerAsset<Material>("4490d8b6facd8234b80d7bb75cdb7d36", "Hover_Edge");
        var halo = control && control.highlightHalo ? control.highlightHalo :
            CatPlayerEditorScope.ExistingPlayerAsset<Material>("b10e013674a7946469b95d853afa3118", "Hover_Halo");
        var sphereRenderer = sphere ? sphere.GetComponent<MeshRenderer>() : null;
        if (sphere && !sphereRenderer) throw new InvalidOperationException("기존 중앙 커서의 MeshRenderer 연결이 없습니다.");
        var cursorMat = sphereRenderer && sphereRenderer.sharedMaterial ? sphereRenderer.sharedMaterial :
            CatPlayerEditorScope.ExistingPlayerAsset<Material>("3fbe3516727071943911271556789ee7", "Cursor_White");
        var cylinder = root ? null : CatPlayerEditorScope.ExistingPlayerAsset<GameObject>("71db0952959b85248a676276c7b152b2", "CarryCylinder");
        var cube = root ? null : CatPlayerEditorScope.ExistingPlayerAsset<GameObject>("d90da7c63d50a5b438e7301341ca231a", "InventoryCube");
        if (!root && (!cylinder.GetComponent<CatInteractionItem>() || !cube.GetComponent<CatInteractionItem>()))
            throw new InvalidOperationException("기존 상호작용 프리팹에 CatInteractionItem 연결이 필요합니다.");

        if (!root)
        {
            root = new GameObject("Cat_Interaction_Items"); Undo.RegisterCreatedObjectUndo(root, "상호작용 아이템 설치");
            for (int i = 0; i < 4; i++) Place(cylinder, root.transform, "Cylinder_" + (i + 1), new Vector3(3.4f + .7f * i, 1.006f, -13.57f));
            for (int i = 0; i < 2; i++) Place(cube, root.transform, "Cube_" + (i + 1), new Vector3(4.1f + 1.1f * i, .169f, -12.35f));
        }
        if (!presenter) presenter = Undo.AddComponent<CatInventoryPickupPresenter>(player);
        if (!control) control = Undo.AddComponent<CatInteractionController>(player);
        if (!cursor) cursor = Undo.AddComponent<CatCenterCursor>(camera.gameObject);
        Undo.RecordObjects(new UnityEngine.Object[] { motor, control, cursor }, "Reconnect interaction");
        control.view = camera; control.pickup = presenter;
        control.highlightFill = fill; control.highlightEdge = edge; control.highlightHalo = halo;
        var equipment = player.GetComponent<CatShotgunEquipment>();
        if (!control.equipment && equipment) control.equipment = equipment;
        if (!sphere)
        {
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "Cat_CenterCursorSphere";
            Undo.RegisterCreatedObjectUndo(sphere, "중앙 구체 커서 설치");
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(camera.transform, false);
            sphere.transform.localPosition = new Vector3(0, 0, .2f); sphere.transform.localScale = Vector3.one * .004f;
            sphereRenderer = sphere.GetComponent<MeshRenderer>();
        }
        Undo.RecordObject(sphereRenderer, "Reconnect cursor material");
        sphereRenderer.sharedMaterial = cursorMat;
        sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sphereRenderer.receiveShadows = false;
        cursor.view = camera; cursor.sphere = sphere.transform; motor.view = camera.transform;
        foreach (var component in new Component[] { motor, control, cursor, sphereRenderer })
        {
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("PlaytestScene01 저장에 실패했습니다. 변경 내용은 Undo로 확인할 수 있습니다.");
        Debug.Log("기존 상호작용 객체와 사용자 설정을 보존하고 연결을 확인했습니다. 새 에셋·meta는 만들지 않았습니다.");
    }
    static void Place(GameObject prefab, Transform parent, string name, Vector3 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = name; go.transform.SetParent(parent); go.transform.position = position;
        go.GetComponent<CatInteractionItem>().itemId = name;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponent<CatInteractionItem>());
        Undo.RegisterCreatedObjectUndo(go, "샘플 배치");
    }
}
