using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>현재 PlayerTestScene의 기존 샷건에 조준·발사를 연결합니다. 기존 이동 클립과 모델은 보존합니다.</summary>
public static class CatCombatSetup
{
    const string ClipPath = "Assets/03Sprites/Player/Animations/Shotgun_Fire.anim";
    const string RingMaterialPath = "Assets/03Sprites/Player/Weapons/Shotgun/Shotgun_SpreadRing.mat";
    [MenuItem("Tools/Cat Player/조준과 샷건 발사 연결")]
    public static void Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/01Scenes/PlayerTestScene.unity")
            throw new InvalidOperationException("PlayerTestScene 편집 모드에서만 연결합니다.");
        var equipment = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CatShotgunEquipment>(true)).Single();
        if (!equipment.weaponRoot || !equipment.leftGrip || !equipment.rightGrip)
            throw new InvalidOperationException("기존 샷건과 양손 잡는 위치를 먼저 연결해야 합니다.");
        var player = equipment.gameObject;
        var motor = player.GetComponent<CatPlayerMotor>();
        var cm = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CinemachineCamera>(true)).Single();
        var pose = player.GetComponent<CatShotgunPose>() ?? Undo.AddComponent<CatShotgunPose>(player);
        var combat = player.GetComponent<CatShotgunCombat>() ?? Undo.AddComponent<CatShotgunCombat>(player);
        var zoom = cm.GetComponent<CatAimZoom>() ?? Undo.AddComponent<CatAimZoom>(cm.gameObject);
        var ring = player.GetComponent<CatShotSpreadRing>() ?? Undo.AddComponent<CatShotSpreadRing>(player);
        Undo.RecordObjects(new UnityEngine.Object[] { equipment, motor, pose, combat, zoom, ring }, "Connect shotgun aiming");

        var muzzle = equipment.weaponRoot.Find("Muzzle");
        if (!muzzle)
        {
            // 메시의 실제 총열 끝(-X)의 점들에서 중심 높이를 측정합니다.
            var vertices = equipment.weaponRoot.GetComponentsInChildren<MeshFilter>(true)
                .SelectMany(m => m.sharedMesh.vertices.Select(v => equipment.weaponRoot.InverseTransformPoint(m.transform.TransformPoint(v)))).ToArray();
            float front = vertices.Min(v => v.x);
            var end = vertices.Where(v => v.x < front + .015f).ToArray();
            var marker = new GameObject("Muzzle"); Undo.RegisterCreatedObjectUndo(marker, "Add muzzle marker");
            muzzle = marker.transform; muzzle.SetParent(equipment.weaponRoot, false);
            muzzle.localPosition = new Vector3(front - .002f, end.Average(v => v.y), end.Average(v => v.z));
            muzzle.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        }
        var guard = player.transform.Find("ShotGuardOrigin");
        if (!guard)
        {
            var marker = new GameObject("ShotGuardOrigin"); Undo.RegisterCreatedObjectUndo(marker, "Add shot guard marker");
            guard = marker.transform; guard.SetParent(player.transform, false);
            guard.localPosition = new Vector3(0, 1.05f, .05f);
        }
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (!clip)
        {
            clip = new AnimationClip { name = "Shotgun_Fire", frameRate = 60 };
            AddCurve(clip, "raiseWeight", new Keyframe(0,0), new Keyframe(.12f,1), new Keyframe(.32f,1), new Keyframe(.5f,0));
            AddCurve(clip, "recoilDistance", new Keyframe(0,0), new Keyframe(.12f,0), new Keyframe(.155f,.09f), new Keyframe(.27f,0), new Keyframe(.5f,0));
            AddCurve(clip, "recoilPitch", new Keyframe(0,0), new Keyframe(.12f,0), new Keyframe(.155f,5), new Keyframe(.28f,0), new Keyframe(.5f,0));
            AssetDatabase.CreateAsset(clip, ClipPath);
            AssetDatabase.SaveAssetIfDirty(clip);
        }
        pose.fireMotion = clip; pose.muzzle = muzzle;
        combat.equipment = equipment; combat.pose = pose; combat.view = motor.view.GetComponent<Camera>();
        combat.muzzle = muzzle; combat.guardOrigin = guard;
        combat.pelletCount = 4; combat.aimedSpreadDegrees = 1.5f;
        equipment.aimingPose = pose; motor.aimSource = combat; zoom.aimSource = combat;
        var ringMaterial = AssetDatabase.LoadAssetAtPath<Material>(RingMaterialPath);
        if (!ringMaterial)
        {
            var shader = Shader.Find("Cat Player/Shot Spread Ring");
            if (!shader || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("산탄 범위 셰이더의 컴파일을 확인해야 합니다.");
            ringMaterial = new Material(shader) { name = "Shotgun_SpreadRing" };
            AssetDatabase.CreateAsset(ringMaterial, RingMaterialPath);
            AssetDatabase.SaveAssetIfDirty(ringMaterial);
        }
        ring.view = combat.view; ring.stateSource = combat; ring.ringMaterial = ringMaterial;
        foreach (var component in new Component[] { equipment, motor, pose, combat, zoom, ring })
        {
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("샷건 줌·걷기 제한·총구 발사·손 준비/반동 클립 연결 완료. PlayerTestScene만 저장했습니다.");
    }
    static void AddCurve(AnimationClip clip, string field, params Keyframe[] keys)
    {
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(CatShotgunPose), field), curve);
    }
}
