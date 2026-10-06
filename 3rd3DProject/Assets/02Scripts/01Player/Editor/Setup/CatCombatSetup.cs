using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

/// <summary>허용 씬의 기존 샷건 에셋과 조준·발사 연결을 복구합니다. 사용자 설정값은 보존합니다.</summary>
public static class CatCombatSetup
{
    [MenuItem("Tools/Cat Player/조준과 샷건 발사 연결")]
    public static void Configure()
    {
        var scene = CatPlayerEditorScope.RequireScene(editMode: true);
        CatPlayerEditorScope.RequireValidationIdle();
        var equipment = CatPlayerEditorScope.FindSingle<CatShotgunEquipment>();
        if (!equipment.IsReady || !equipment.weaponRoot || !equipment.leftGrip || !equipment.rightGrip)
            throw new InvalidOperationException("기존 샷건과 양손 잡는 위치를 먼저 연결해야 합니다.");
        var player = equipment.gameObject;
        var motor = player.GetComponent<CatPlayerMotor>();
        var cm = CatPlayerEditorScope.FindSingle<CinemachineCamera>();
        CatPlayerEditorScope.RequireOwned(equipment, motor, equipment.weaponRoot, equipment.leftGrip, equipment.rightGrip);
        var view = CatPlayerEditorScope.OutputCamera(motor);
        var pose = player.GetComponent<CatShotgunPose>();
        var combat = player.GetComponent<CatShotgunCombat>();
        var zoom = cm.GetComponent<CatAimZoom>();
        var ring = player.GetComponent<CatShotSpreadRing>();
        var clip = pose && pose.fireMotion ? pose.fireMotion :
            CatPlayerEditorScope.ExistingPlayerAsset<AnimationClip>("8c5ced8c68c5055498bdcb05eba38854", "Shotgun_Fire");
        var ringMaterial = ring && ring.ringMaterial ? ring.ringMaterial :
            CatPlayerEditorScope.ExistingPlayerAsset<Material>("1d362b368efc607499ea48107dac9de1", "Shotgun_SpreadRing");
        if (!ringMaterial.shader || ShaderUtil.ShaderHasError(ringMaterial.shader))
            throw new InvalidOperationException("기존 산탄 범위 재질의 셰이더를 확인하세요.");

        var muzzle = combat && combat.muzzle ? combat.muzzle :
            pose && pose.muzzle ? pose.muzzle : equipment.weaponRoot.Find("Muzzle");
        var guard = combat && combat.guardOrigin ? combat.guardOrigin : player.transform.Find("ShotGuardOrigin");
        if (muzzle)
        {
            CatPlayerEditorScope.RequireOwned(muzzle);
            if (muzzle == equipment.weaponRoot || !muzzle.IsChildOf(equipment.weaponRoot))
                throw new InvalidOperationException("기존 총구는 현재 샷건 아래에 있어야 합니다.");
        }
        if (guard)
        {
            CatPlayerEditorScope.RequireOwned(guard);
            if (guard == player.transform || !guard.IsChildOf(player.transform))
                throw new InvalidOperationException("기존 차단 시작점은 현재 플레이어 아래에 있어야 합니다.");
        }
        Vector3 muzzlePosition = default;
        if (!muzzle)
        {
            // 실제 모델이 없으면 연결 전에 중단합니다. 새 모델이나 에셋으로 대체하지 않습니다.
            var vertices = equipment.weaponRoot.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.sharedMesh)
                .SelectMany(m => m.sharedMesh.vertices.Select(v => equipment.weaponRoot.InverseTransformPoint(m.transform.TransformPoint(v)))).ToArray();
            if (vertices.Length == 0) throw new InvalidOperationException("총구를 계산할 기존 샷건 메시가 없습니다.");
            float front = vertices.Min(v => v.x);
            var end = vertices.Where(v => v.x < front + .015f).ToArray();
            muzzlePosition = new Vector3(front - .002f, end.Average(v => v.y), end.Average(v => v.z));
        }

        if (!pose) pose = Undo.AddComponent<CatShotgunPose>(player);
        if (!combat) combat = Undo.AddComponent<CatShotgunCombat>(player); // 최초 추가 시 컴포넌트 기본값 사용
        if (!zoom) zoom = Undo.AddComponent<CatAimZoom>(cm.gameObject);
        if (!ring) ring = Undo.AddComponent<CatShotSpreadRing>(player);
        Undo.RecordObjects(new UnityEngine.Object[] { equipment, motor, pose, combat, zoom, ring }, "Connect shotgun aiming");
        if (!muzzle)
        {
            var marker = new GameObject("Muzzle"); Undo.RegisterCreatedObjectUndo(marker, "Add muzzle marker");
            muzzle = marker.transform; muzzle.SetParent(equipment.weaponRoot, false);
            muzzle.localPosition = muzzlePosition;
            muzzle.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        }
        if (!guard)
        {
            var marker = new GameObject("ShotGuardOrigin"); Undo.RegisterCreatedObjectUndo(marker, "Add shot guard marker");
            guard = marker.transform; guard.SetParent(player.transform, false);
            guard.localPosition = new Vector3(0, 1.05f, .05f);
        }
        pose.fireMotion = clip; pose.muzzle = muzzle;
        combat.equipment = equipment; combat.pose = pose; combat.view = view;
        combat.muzzle = muzzle; combat.guardOrigin = guard;
        equipment.aimingPose = pose; motor.view = view.transform; motor.aimSource = combat; zoom.aimSource = combat;
        ring.view = combat.view; ring.stateSource = combat; ring.ringMaterial = ringMaterial;
        foreach (var component in new Component[] { equipment, motor, pose, combat, zoom, ring })
        {
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("PlaytestScene01 저장에 실패했습니다. 변경 내용은 Undo로 확인할 수 있습니다.");
        Debug.Log("기존 에셋으로 조준·샷건 발사를 연결했습니다. 산탄 수·퍼짐·자세 설정을 보존하고 PlaytestScene01만 저장했습니다.");
    }
}
