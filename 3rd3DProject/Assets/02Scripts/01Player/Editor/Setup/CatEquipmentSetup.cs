using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>기존 샷건과 61번의 최종 잡는 자세를 연결하는 수동 설치·재연결 도구.</summary>
public static class CatEquipmentSetup
{
    const string Folder = "Assets/03Sprites/Player/Weapons/Shotgun";
    const string ScenePath = "Assets/01Scenes/PlayerTestScene.unity";

    [MenuItem("Tools/Cat Player/샷건 연결 확인 및 설치")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("PlayerTestScene 편집 모드에서만 실행합니다.");
        var player = GameObject.Find("Cat_Player");
        if (!player) throw new InvalidOperationException("Cat_Player가 없습니다.");
        var interaction = player.GetComponent<CatInteractionController>();
        if (!interaction) throw new InvalidOperationException("상호작용 컴포넌트가 없습니다.");
        var bones = player.GetComponentsInChildren<Transform>(true);
        var motion = bones.Single(t => t.name == "CTRL_Squash");
        var left = bones.Single(t => t.name == "DEF_Hand.L");
        var right = bones.Single(t => t.name == "DEF_Hand.R");
        var equipment = player.GetComponent<CatShotgunEquipment>();
        if (equipment && equipment.weaponRoot)
        {
            // 이미 조절한 장착 위치·자세는 재실행으로 덮어쓰지 않습니다.
            Undo.RecordObject(equipment, "Reconnect shotgun");
            Undo.RecordObject(interaction, "Reconnect shotgun");
            interaction.equipment = equipment;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("기존 샷건 설정을 보존하고 연결만 확인했습니다."); return;
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Shotgun_Ready.prefab");
        if (!prefab) prefab = CreatePrefab();
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add existing shotgun");
        instance.name = "Cat_Shotgun";
        instance.transform.localPosition = new Vector3(.3f, .975f, .745f);
        instance.transform.localRotation = HoldRotation;
        instance.transform.localScale = Vector3.one;
        instance.SetActive(false);
        if (!equipment) equipment = Undo.AddComponent<CatShotgunEquipment>(player);
        Undo.RecordObject(equipment, "Connect shotgun");
        Undo.RecordObject(interaction, "Connect shotgun");
        equipment.weaponRoot = instance.transform;
        equipment.motionRoot = motion;
        equipment.leftHand = left; equipment.rightHand = right;
        equipment.leftGrip = instance.transform.Find("LeftGrip");
        equipment.rightGrip = instance.transform.Find("RightGrip");
        equipment.weaponLocalPosition = instance.transform.localPosition;
        equipment.weaponLocalEulerAngles = instance.transform.localEulerAngles;
        interaction.equipment = equipment;
        EditorUtility.SetDirty(equipment); EditorUtility.SetDirty(interaction);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("기존 샷건 모델·양손 잡는 자세 연결 완료. 소환 애니메이션·효과는 가져오지 않았습니다.");
    }

    static Quaternion HoldRotation => Quaternion.FromToRotation(Vector3.left, new Vector3(-.88f, .035f, .475f).normalized);

    static GameObject CreatePrefab()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Shotgun01_RedBlue.fbx");
        if (!source) throw new InvalidOperationException("기존 Shotgun01_RedBlue.fbx를 먼저 지정 폴더에 넣어 주세요.");
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) throw new InvalidOperationException("URP Unlit 셰이더를 찾을 수 없습니다.");
        var root = new GameObject("Shotgun_Ready");
        try
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            visual.name = "Visual";
            // 원본의 손잡이 피벗을 사용하고 전시장 위치만 제거합니다. FBX 축 보정 회전은 유지합니다.
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale *= .38f;
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var imported = materials[i];
                    string path = Folder + "/" + imported.name + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!material)
                    {
                        material = new Material(shader) { name = imported.name };
                        // Blender의 선형 diffuse 색을 Unity 색상 필드의 sRGB 값으로 옮깁니다.
                        material.SetColor("_BaseColor", imported.color.gamma);
                        AssetDatabase.CreateAsset(material, path);
                        AssetDatabase.SaveAssetIfDirty(material);
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            AddGrip(root.transform, "RightGrip", new Vector3(.3f, .92f, .73f));
            AddGrip(root.transform, "LeftGrip", new Vector3(-.0853167f, .884804f, .9526972f));
            return PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Shotgun_Ready.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void AddGrip(Transform parent, string name, Vector3 handPosition)
    {
        var grip = new GameObject(name).transform;
        grip.SetParent(parent, false);
        grip.localPosition = Quaternion.Inverse(HoldRotation) * (handPosition - new Vector3(.3f, .975f, .745f));
        // 둥근 손의 원래 캐릭터 축 방향을 유지합니다.
        grip.localRotation = Quaternion.Inverse(HoldRotation);
    }
}
