using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>먼 임시 공간에서 실제 PhysX 질의로 발사 판정을 검사합니다. 씬·에셋을 저장하지 않습니다.</summary>
public static class HitscanValidation
{
    /// <summary>PlaytestScene01의 Play 모드에서만 실행하며 실패하면 근거를 담은 예외를 발생시킵니다.</summary>
    public static string Run()
    {
        Scene scene = PlayerEditorScope.RequireScene();
        PlayerEditorScope.RequireValidationIdle();
        if (!EditorApplication.isPlaying || !Application.isPlaying)
            throw new InvalidOperationException("PlaytestScene01의 Play 모드에서만 히트스캔 검증을 실행할 수 있습니다.");

        bool wasDirty = scene.isDirty;
        var report = new StringBuilder("HitscanValidation | 실제 물리 질의 | 씬·에셋 저장 없음\n");
        GameObject root = null;
        int checks = 0;
        PlayerEditorScope.BeginValidation(nameof(HitscanValidation));
        try
        {
            root = NewObject("__CatHitscanValidation_Temporary", null);
            root.transform.position = new Vector3(1000f, 1000f, 1000f);
            GameObject owner = NewObject("Owner", root.transform);
            BoxCollider self = owner.AddComponent<BoxCollider>();
            self.size = Vector3.one;
            self.enabled = false;
            Transform muzzle = NewObject("Muzzle", owner.transform).transform;
            muzzle.localPosition = new Vector3(0f, 0f, 1.2f);
            GameObject viewObject = NewObject("DisabledTestCamera", owner.transform);
            viewObject.transform.localPosition = new Vector3(0f, 0f, -4f);
            Camera view = viewObject.AddComponent<Camera>();
            view.enabled = false;
            // 1000m 떨어진 검증 공간에서 근평면 역투영의 float 오차를 줄입니다.
            // 실제 플레이어 카메라는 변경하지 않으며 명중 허용 오차도 유지합니다.
            view.nearClipPlane = 0.3f;
            view.farClipPlane = 100f;
            view.fieldOfView = 60f;
            var query = new HitscanQuery(owner.transform);
            Vector3 guard = owner.transform.position;
            BoxCollider target = Box(root.transform, "ForwardTarget", new Vector3(0f, 0f, 12f), Vector3.one * 2f);
            Physics.SyncTransforms();

            bool aimed = query.TryGetAimPoint(view, 25f, ~0, out Vector3 aim);
            ShotHit shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(aimed && Near(aim, guard + new Vector3(0f, 0f, 11f)) &&
                shot.Kind == ShotHitKind.Hit && shot.Collider == target && Mathf.Abs(shot.Distance - 9.8f) < 0.02f,
                "01 화면 중앙 조준점과 실제 표적 명중·거리", report, ref checks);

            self.enabled = true;
            // 총구 앞까지 소유자 충돌체를 늘려도 자기 자신을 맞히지 않아야 합니다.
            self.size = new Vector3(1f, 1f, 4f);
            Box(owner.transform, "OwnerChildCollider", new Vector3(0f, 0f, 2.5f), Vector3.one * 0.2f);
            Physics.SyncTransforms();
            aimed = query.TryGetAimPoint(view, 25f, ~0, out aim);
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(aimed && Near(aim, guard + new Vector3(0f, 0f, 11f)) &&
                shot.Kind == ShotHitKind.Hit && shot.Collider == target,
                "02 카메라·총구 질의 모두 소유자와 자식 충돌체 제외", report, ref checks);

            BoxCollider trigger = Box(root.transform, "IgnoredTrigger", new Vector3(0f, 0f, 3f), Vector3.one);
            trigger.isTrigger = true;
            Physics.SyncTransforms();
            aimed = query.TryGetAimPoint(view, 25f, ~0, out aim);
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(aimed && Near(aim, guard + new Vector3(0f, 0f, 11f)) &&
                shot.Kind == ShotHitKind.Hit && shot.Collider == target,
                "03 앞쪽 Trigger가 조준·발사를 가로막지 않음", report, ref checks);

            BoxCollider nearer = Box(root.transform, "NearestWall", new Vector3(0f, 0f, 5f), Vector3.one);
            Physics.SyncTransforms();
            aimed = query.TryGetAimPoint(view, 25f, ~0, out aim);
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(aimed && Near(aim, guard + new Vector3(0f, 0f, 4.5f)) &&
                shot.Kind == ShotHitKind.Hit && shot.Collider == nearer && shot.Normal.z < -0.9f,
                "04 여러 장애물 중 가장 앞의 벽·충돌 법선 선택", report, ref checks);
            nearer.gameObject.SetActive(false);
            trigger.gameObject.SetActive(false);

            BoxCollider side = Box(root.transform, "SideTarget", new Vector3(8f, 0f, 1.2f), Vector3.one);
            muzzle.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.Hit && shot.Collider == side &&
                Vector3.Dot(shot.Direction, Vector3.right) > 0.999f && Near(shot.Origin, muzzle.position),
                "05 카메라 방향과 달라도 실제 총구 위치·방향으로 발사", report, ref checks);
            side.gameObject.SetActive(false);
            muzzle.localRotation = Quaternion.identity;

            Vector3 spreadDirection = new Vector3(1f, 0f, 1f).normalized;
            BoxCollider spreadTarget = Box(root.transform, "SpreadTarget",
                muzzle.localPosition + spreadDirection * 8f, Vector3.one);
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 25f, ~0, new Vector2(45f, 0f));
            Check(shot.Kind == ShotHitKind.Hit && shot.Collider == spreadTarget &&
                Vector3.Dot(shot.Direction, spreadDirection) > 0.999f,
                "06 지정한 좌우 퍼짐 각도로 옆 표적 명중", report, ref checks);
            spreadTarget.gameObject.SetActive(false);

            BoxCollider wall = Box(root.transform, "BarrelObstruction", new Vector3(0f, 0f, 0.6f),
                new Vector3(0.6f, 0.6f, 0.08f));
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.Blocked && shot.Collider == wall,
                "07 가슴과 총구 사이 얇은 벽 차단", report, ref checks);

            wall.transform.localPosition = muzzle.localPosition;
            wall.size = Vector3.one * 0.4f;
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.Blocked && shot.Collider == wall,
                "08 총구가 벽 내부에 들어가도 원거리 표적 미명중", report, ref checks);

            wall.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            wall.size = new Vector3(0.6f, 0.6f, 0.04f);
            muzzle.localPosition = new Vector3(0f, 0f, 3f);
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.Blocked && shot.Collider == wall,
                "09 총구가 얇은 벽을 완전히 넘어가도 관통 발사 차단", report, ref checks);
            muzzle.localPosition = new Vector3(0f, 0f, 1.2f);

            wall.transform.localPosition = Vector3.zero;
            wall.size = Vector3.one * 0.4f;
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.Blocked && shot.Collider == wall,
                "10 가슴 검사 시작점이 벽 내부인 경우도 차단", report, ref checks);

            wall.transform.localPosition = new Vector3(0.2f, 0f, 0.6f);
            wall.size = new Vector3(0.38f, 0.3f, 0.3f);
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.Blocked && shot.Collider == wall,
                "11 중심선은 비어 있어도 총열 반지름에 벽이 닿으면 차단", report, ref checks);
            wall.gameObject.SetActive(false);
            target.gameObject.SetActive(false);
            Physics.SyncTransforms();

            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            aimed = query.TryGetAimPoint(view, 25f, ~0, out aim);
            Vector3 expectedAim = view.transform.position + view.transform.forward * (view.nearClipPlane + 25f);
            Check(shot.Kind == ShotHitKind.Miss && shot.Collider == null &&
                Near(shot.Point, muzzle.position + Vector3.forward * 25f) &&
                Mathf.Abs(shot.Distance - 25f) < 0.001f && aimed && Near(aim, expectedAim),
                "12 빈 공간에서는 총구·카메라 각각 사거리 끝점 반환", report, ref checks);

            target.gameObject.SetActive(true);
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 2f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.Miss && Mathf.Abs(shot.Distance - 2f) < 0.001f,
                "13 사거리 밖 표적은 맞히지 않음", report, ref checks);

            target.gameObject.layer = 2;
            Physics.SyncTransforms();
            ShotHit excluded = query.Cast(guard, muzzle, 25f, 1 << 0, Vector2.zero);
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(excluded.Kind == ShotHitKind.Miss && shot.Kind == ShotHitKind.Hit && shot.Collider == target,
                "14 지정 레이어 제외 및 전체 마스크 포함 처리", report, ref checks);
            target.gameObject.layer = 0;

            // 총구와 가슴이 같은 점이어도 길이 0인 방향을 물리 API에 전달하면 안 됩니다.
            shot = query.Cast(muzzle.position, muzzle, 25f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.Hit && shot.Collider == target,
                "15 가슴·총구 검사 구간 길이 0 처리", report, ref checks);

            GameObject crowded = NewObject("RayBufferCrowding", root.transform);
            for (int i = 0; i < 70; i++)
                Box(crowded.transform, "RayCrowd_" + i, new Vector3(0f, 0f, 5f + i * 0.06f),
                    new Vector3(0.2f, 0.2f, 0.02f));
            Physics.SyncTransforms();
            aimed = query.TryGetAimPoint(view, 25f, ~0, out aim);
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(!aimed && shot.Kind == ShotHitKind.QueryOverflow && shot.Collider == null,
                "16 광선 검사 버퍼 포화 시 잘못된 원거리 명중 차단", report, ref checks);
            crowded.SetActive(false);

            GameObject overlapping = NewObject("OverlapBufferCrowding", root.transform);
            for (int i = 0; i < 70; i++)
                Box(overlapping.transform, "OverlapCrowd_" + i, Vector3.zero, Vector3.one * 0.1f);
            Physics.SyncTransforms();
            shot = query.Cast(guard, muzzle, 25f, ~0, Vector2.zero);
            Check(shot.Kind == ShotHitKind.QueryOverflow && shot.Collider == null,
                "17 겹침 검사 버퍼 포화 시 안전 차단", report, ref checks);
            overlapping.SetActive(false);
            Physics.SyncTransforms();

            bool invalidBlocked = true;
            float[] invalidRanges = { 0f, -1f, float.NaN, float.PositiveInfinity };
            foreach (float range in invalidRanges)
            {
                invalidBlocked &= query.Cast(guard, muzzle, range, ~0, Vector2.zero).Kind == ShotHitKind.Blocked;
                invalidBlocked &= !query.TryGetAimPoint(view, range, ~0, out aim);
            }
            invalidBlocked &= query.Cast(new Vector3(float.NaN, 0f, 0f), muzzle, 25f, ~0, Vector2.zero).Kind == ShotHitKind.Blocked;
            invalidBlocked &= query.Cast(guard, null, 25f, ~0, Vector2.zero).Kind == ShotHitKind.Blocked;
            invalidBlocked &= query.Cast(guard, muzzle, 25f, ~0, new Vector2(float.NaN, 0f)).Kind == ShotHitKind.Blocked;
            invalidBlocked &= query.Cast(guard, muzzle, 25f, ~0, Vector2.zero, -0.1f).Kind == ShotHitKind.Blocked;
            invalidBlocked &= !query.TryGetAimPoint(null, 25f, ~0, out aim);
            Check(invalidBlocked, "18 잘못된 사거리·위치·총구·카메라·퍼짐·반지름 입력 방어", report, ref checks);
        }
        catch (Exception error)
        {
            throw new InvalidOperationException(report + "FAIL: " + error.Message, error);
        }
        finally
        {
            try
            {
                // 이 함수가 만든 단일 루트만 제거합니다. 기존 오브젝트·씬 파일에는 접근하지 않습니다.
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                Physics.SyncTransforms();
            }
            finally { PlayerEditorScope.EndValidation(nameof(HitscanValidation)); }
        }

        if (scene.isDirty != wasDirty)
            throw new InvalidOperationException(report + "FAIL: 실행 전후 씬 dirty 상태가 달라졌습니다.");
        report.AppendLine("RESULT: " + checks + " PASS | 임시 루트 제거 완료 | 씬 dirty 상태 유지");
        return report.ToString();
    }

    static GameObject NewObject(string name, Transform parent)
    {
        var created = new GameObject(name) { hideFlags = HideFlags.DontSave };
        if (parent != null) created.transform.SetParent(parent, false);
        return created;
    }

    static BoxCollider Box(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject created = NewObject(name, parent);
        created.transform.localPosition = position;
        BoxCollider collider = created.AddComponent<BoxCollider>();
        collider.size = size;
        return collider;
    }

    static bool Near(Vector3 first, Vector3 second) => Vector3.Distance(first, second) < 0.02f;

    static void Check(bool condition, string label, StringBuilder report, ref int checks)
    {
        if (!condition) throw new InvalidOperationException(label);
        checks++;
        report.AppendLine("PASS: " + label);
    }
}
