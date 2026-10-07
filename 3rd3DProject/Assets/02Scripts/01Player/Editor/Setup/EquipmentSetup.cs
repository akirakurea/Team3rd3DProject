using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>기존 샷건 프리팹과 양손 기준점을 연결하는 수동 재연결 도구입니다.</summary>
public static class EquipmentSetup
{
    [MenuItem("Tools/Cat Player/샷건 연결 확인 및 설치")]
    public static void Configure()
    {
        var scene = PlayerEditorScope.RequireScene(editMode: true);
        PlayerEditorScope.RequireValidationIdle();
        var player = PlayerEditorScope.FindSingle<PlayerController>().gameObject;
        var interaction = player.GetComponent<InteractionController>();
        if (!interaction) throw new InvalidOperationException("상호작용 컴포넌트가 없습니다.");
        var bones = player.GetComponentsInChildren<Transform>(true);
        var motion = bones.Single(t => t.name == "CTRL_Squash");
        var left = bones.Single(t => t.name == "DEF_Hand.L");
        var right = bones.Single(t => t.name == "DEF_Hand.R");
        var equipment = player.GetComponent<ShotgunEquipment>();
        Transform existingWeapon = equipment ? equipment.weaponRoot : null;
        if (!existingWeapon)
        {
            // 참조만 끊어진 중첩 프리팹을 먼저 찾습니다. 양손 기준점은 총 루트의 직계 자식입니다.
            var candidates = bones.Where(bone => bone != player.transform &&
                bone.Find("LeftGrip") != null && bone.Find("RightGrip") != null).ToArray();
            if (candidates.Length > 1)
                throw new InvalidOperationException("기존 샷건 후보가 여러 개입니다. 자동 선택하거나 새 총을 추가하지 않습니다.");
            if (candidates.Length == 1) existingWeapon = candidates[0];
        }
        bool hasWeapon = existingWeapon;
        var prefab = hasWeapon ? null :
            PlayerEditorScope.ExistingPlayerAsset<GameObject>("6f328856e4b97b940b49b718fd5eee4e", "Shotgun_Ready");
        var weapon = hasWeapon ? existingWeapon : prefab.transform;
        var leftGrip = weapon.Find("LeftGrip");
        var rightGrip = weapon.Find("RightGrip");
        if (!leftGrip || !rightGrip)
            throw new InvalidOperationException("기존 샷건에 LeftGrip·RightGrip 기준점이 필요합니다.");
        if (hasWeapon)
        {
            PlayerEditorScope.RequireOwned(weapon, leftGrip, rightGrip);
            if (weapon == player.transform || !weapon.IsChildOf(player.transform))
                throw new InvalidOperationException("기존 샷건은 현재 플레이어 아래에 있어야 합니다.");
        }

        bool newEquipment = !equipment;
        if (!equipment) equipment = Undo.AddComponent<ShotgunEquipment>(player);
        Undo.RecordObjects(new UnityEngine.Object[] { equipment, interaction }, "Reconnect shotgun");
        if (!hasWeapon)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Connect existing shotgun prefab");
            instance.name = "Cat_Shotgun";
            if (newEquipment)
            {
                equipment.weaponLocalPosition = new Vector3(.3f, .975f, .745f);
                equipment.weaponLocalEulerAngles = HoldRotation.eulerAngles;
            }
            instance.transform.localPosition = equipment.weaponLocalPosition;
            instance.transform.localEulerAngles = equipment.weaponLocalEulerAngles;
            instance.transform.localScale = Vector3.one;
            instance.SetActive(false);
            weapon = instance.transform;
            leftGrip = instance.transform.Find("LeftGrip");
            rightGrip = instance.transform.Find("RightGrip");
        }
        // 기존 총을 찾았을 때는 위치·회전·크기와 이미 조정한 장비 자세값을 그대로 둡니다.
        equipment.weaponRoot = weapon;
        equipment.motionRoot = motion;
        equipment.leftHand = left; equipment.rightHand = right;
        equipment.leftGrip = leftGrip; equipment.rightGrip = rightGrip;
        interaction.equipment = equipment;
        EditorUtility.SetDirty(equipment); EditorUtility.SetDirty(interaction);
        PrefabUtility.RecordPrefabInstancePropertyModifications(equipment);
        PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("PlaytestScene01 저장에 실패했습니다. 변경 내용은 Undo로 확인할 수 있습니다.");
        Debug.Log("기존 샷건·양손 연결을 확인했습니다. 기존 자세값과 에셋·meta는 보존했습니다.");
    }

    static Quaternion HoldRotation => Quaternion.FromToRotation(Vector3.left, new Vector3(-.88f, .035f, .475f).normalized);

}
