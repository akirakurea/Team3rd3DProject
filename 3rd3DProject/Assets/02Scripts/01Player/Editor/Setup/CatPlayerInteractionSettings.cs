using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>허용 씬의 기본 물리 재질과 카메라 정책 연결만 설정합니다.</summary>
public static class CatPlayerInteractionSettings
{
    [MenuItem("Tools/Cat Player/무마찰·정지 카메라 설정 연결")]
    public static void Apply()
    {
        var scene = CatPlayerEditorScope.RequireScene(editMode: true);
        CatPlayerEditorScope.RequireValidationIdle();
        var motor = CatPlayerEditorScope.FindSingle<CatPlayerMotor>();
        var player = motor.gameObject;
        var camera = CatPlayerEditorScope.FindSingle<CinemachineCamera>().gameObject;
        var material = CatPlayerEditorScope.ExistingPlayerAsset<PhysicsMaterial>(
            "4c50fb07f0d15164e8b9d935449ba29b", "Player_NoFriction");
        if (material.staticFriction != 0 || material.dynamicFriction != 0 || material.frictionCombine != PhysicsMaterialCombine.Multiply)
            throw new InvalidOperationException("기존 무마찰 재질의 마찰값을 확인하세요. 이 연결 도구는 재질 에셋을 덮어쓰지 않습니다.");
        var body = player.GetComponent<Rigidbody>();
        if (!body) throw new InvalidOperationException("플레이어 Rigidbody 연결이 필요합니다.");
        var colliders = player.GetComponentsInChildren<Collider>(true)
            .Where(collider => !collider.isTrigger && collider.attachedRigidbody == body).ToArray();
        if (colliders.Length == 0) throw new InvalidOperationException("플레이어 Rigidbody에 연결된 충돌체가 없습니다.");
        var limit = camera.GetComponent<CatCameraOrbitLimit>();
        Undo.RecordObject(body, "Remove player damping"); body.linearDamping = 0;
        foreach (var collider in colliders)
        { Undo.RecordObject(collider, "Apply no friction"); collider.sharedMaterial = material; }
        if (!limit) limit = Undo.AddComponent<CatCameraOrbitLimit>(camera);
        Undo.RecordObject(limit, "Connect camera orbit policy");
        limit.motionSource = motor; // 최초 추가 시 기본 60도, 기존 사용자 각도와 보유 거리는 유지합니다.
        PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        PrefabUtility.RecordPrefabInstancePropertyModifications(limit);
        foreach (var collider in colliders) PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("PlaytestScene01 저장에 실패했습니다. 변경 내용은 Undo로 확인할 수 있습니다.");
        Debug.Log("플레이어 충돌체 " + colliders.Length + "개 무마찰·카메라 연결 완료. 장비 연결·회전 범위·보유 거리·기존 에셋을 보존했습니다.");
    }
}
