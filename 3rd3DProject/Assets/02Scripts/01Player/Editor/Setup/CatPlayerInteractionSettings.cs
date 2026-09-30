using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>허용 씬의 기본 물리 재질과 카메라 정책 연결만 설정합니다.</summary>
public static class CatPlayerInteractionSettings
{
    [MenuItem("Tools/Cat Player/무마찰·정지 카메라 설정 연결")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/01Scenes/PlayerTestScene.unity")
            throw new InvalidOperationException("PlayerTestScene 편집 모드 전용입니다.");
        var player = GameObject.Find("Cat_Player");
        var camera = GameObject.Find("Cat_CinemachineCamera");
        if (!player || !camera) throw new InvalidOperationException("플레이어 또는 카메라 연결을 찾을 수 없습니다.");
        const string folder = "Assets/03Sprites/Player/Physics";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/03Sprites/Player", "Physics");
        const string path = folder + "/Player_NoFriction.physicMaterial";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (!material) { material = new PhysicsMaterial("Player_NoFriction"); AssetDatabase.CreateAsset(material, path); }
        material.staticFriction = 0; material.dynamicFriction = 0; material.bounciness = 0;
        material.frictionCombine = PhysicsMaterialCombine.Multiply;
        material.bounceCombine = PhysicsMaterialCombine.Minimum;
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
        var body = player.GetComponent<Rigidbody>();
        if (!body) throw new InvalidOperationException("플레이어 Rigidbody가 없습니다.");
        Undo.RecordObject(body, "Remove player damping"); body.linearDamping = 0;
        int count = 0;
        foreach (var collider in player.GetComponentsInChildren<Collider>(true))
            if (!collider.isTrigger && collider.attachedRigidbody == body)
            { Undo.RecordObject(collider, "Apply no friction"); collider.sharedMaterial = material; count++; }
        var limit = camera.GetComponent<CatCameraOrbitLimit>();
        if (!limit) limit = Undo.AddComponent<CatCameraOrbitLimit>(camera);
        Undo.RecordObject(limit, "Connect camera orbit policy");
        limit.motionSource = player.GetComponent<CatPlayerMotor>(); limit.stationaryHalfAngle = 60;
        var control = player.GetComponent<CatInteractionController>();
        Undo.RecordObject(control, "Configure centered carry"); control.holdDistance = .8f;
        control.equipment = player.GetComponent<CatShotgunEquipment>();
        PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        PrefabUtility.RecordPrefabInstancePropertyModifications(control);
        foreach (var collider in player.GetComponentsInChildren<Collider>(true))
            if (collider.attachedRigidbody == body) PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("플레이어 충돌체 " + count + "개 무마찰 / 정지 카메라 좌우60 / 중앙 들기 연결 완료");
    }
}
