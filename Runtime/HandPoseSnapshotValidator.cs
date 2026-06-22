using System.Text;
using UnityEngine;

// HandPose 스냅샷(Attach/프리팹)이 올바른 계층·좌표계를 갖췄는지 검증한다.
// Attach 직후·프리팹 저장 전후에 호출해 위치 어긋남을 조기에 차단한다.
public static class HandPoseSnapshotValidator
{
    const float k_PosEpsilon = 0.001f;
    const float k_RotEpsilon = 0.1f;
    // 손목·손가락 본 world 거리(m). bakeReferenceScale 적용 후에도 이 값을 넘으면 잘못 저장됐을 가능성.
    const float k_MaxWristWorldFromRoot = 0.5f;
    const float k_MaxFingerSegmentLocal = 0.15f;

    public readonly struct Result
    {
        public readonly bool Valid;
        public readonly string Message;

        public Result(bool valid, string message)
        {
            Valid = valid;
            Message = message;
        }

        public static Result Ok(string message = "OK") => new Result(true, message);
        public static Result Fail(string message) => new Result(false, message);
    }

    public static bool IsMeshContainer(Transform t)
    {
        if (t.GetComponent<SkinnedMeshRenderer>() != null)
            return true;
        if (t.GetComponent<MeshRenderer>() != null && t.GetComponent<MeshFilter>() != null)
            return true;
        return t.name == "LeftHand" || t.name == "RightHand";
    }

    public static Result Validate(GameObject hand, Transform expectedParent = null)
    {
        if (hand == null)
            return Result.Fail("hand가 null입니다.");

        var root = hand.transform;
        var errors = new StringBuilder();

        if (hand.GetComponent<HandPoseSnapshotRoot>() == null)
            errors.AppendLine("- HandPoseSnapshotRoot 컴포넌트가 없습니다.");

        if (expectedParent != null && root.parent != expectedParent)
            errors.AppendLine($"- 부모가 '{expectedParent.name}'이 아닙니다 (현재: {(root.parent ? root.parent.name : "<none>")}).");

        if (root.parent != null && !IsNearZero(root.localPosition))
            errors.AppendLine($"- 루트 localPosition이 (0,0,0)이 아닙니다: {root.localPosition}");

        if (root.parent != null && !IsNearIdentity(root.localRotation))
            errors.AppendLine($"- 루트 localRotation이 identity가 아닙니다: {root.localEulerAngles}");

        var snapshotRoot = hand.GetComponent<HandPoseSnapshotRoot>();
        bool meshFrozen = snapshotRoot != null && snapshotRoot.MeshFrozen;

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!IsMeshContainer(t))
                continue;

            if (meshFrozen)
                continue;

            if (!IsNearZero(t.localPosition))
                errors.AppendLine($"- 메시 컨테이너 '{t.name}' localPosition ≠ 0: {t.localPosition} (추적 원점 오프셋이 남았을 수 있음)");
        }

        foreach (var c in hand.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (c == null)
                continue;
            string ns = c.GetType().Namespace;
            if (!string.IsNullOrEmpty(ns) && ns.StartsWith("UnityEngine.XR.Hands"))
                errors.AppendLine($"- 추적 컴포넌트가 남아 있습니다: {c.GetType().Name}");
        }

        if (hand.GetComponentInChildren<SkinnedMeshRenderer>(true) == null
            && hand.GetComponentInChildren<MeshFilter>(true)?.sharedMesh == null)
            errors.AppendLine("- 손 메시가 없습니다 (SkinnedMeshRenderer 또는 baked MeshFilter).");

        if (snapshotRoot == null || !snapshotRoot.MeshFrozen)
            ValidateWristAndFingerScale(root, errors);
        else
            ValidateFrozenMeshBounds(hand, errors);

        if (errors.Length > 0)
            return Result.Fail(errors.ToString().TrimEnd());

        return Result.Ok("루트·메시·컴포넌트 검증 통과");
    }

    static void ValidateFrozenMeshBounds(GameObject hand, StringBuilder errors)
    {
        var mesh = hand.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
        if (mesh == null)
            return;

        // 정상 baked hand local bounds ~0.12–0.25m. 0.1 scale 타깃에서 잘못 bake하면 ~1–2m.
        if (mesh.bounds.size.magnitude > 0.45f)
        {
            errors.AppendLine(
                $"- baked mesh bounds={mesh.bounds.size} (local) — Attach to Target을 다시 실행하세요 (이전 bake 순서 버그)");
        }
    }

    static void ValidateWristAndFingerScale(Transform root, StringBuilder errors)
    {
        var wrist = FindBone(root, "L_Wrist") ?? FindBone(root, "R_Wrist");
        if (wrist == null)
        {
            errors.AppendLine("- L_Wrist / R_Wrist 본을 찾지 못했습니다.");
            return;
        }

        float bakeScale = GetBakeReferenceScale(root);
        float wristWorldDist = EstimateWorldDistance(root, root, wrist, bakeScale);
        if (wristWorldDist > k_MaxWristWorldFromRoot)
        {
            errors.AppendLine(
                $"- 손목이 루트에서 너무 멉니다 (world {wristWorldDist:F2}m, local {wrist.localPosition}, bakeScale {bakeScale:F3}). " +
                "Attach to Target 없이 저장됐을 수 있습니다.");
        }

        // 손가락 proximal segment — live hand ~0.06m. bakeScale 미적용 local만 보면 0.1 타깃에서 ~0.6m로 오탐.
        foreach (var finger in new[] { "L_IndexProximal", "L_MiddleProximal", "R_IndexProximal", "R_MiddleProximal" })
        {
            var bone = FindBone(root, finger);
            if (bone == null || bone.parent == null)
                continue;

            float worldLen = EstimateWorldDistance(root, bone.parent, bone, bakeScale);
            if (worldLen > k_MaxFingerSegmentLocal)
            {
                errors.AppendLine(
                    $"- '{finger}' world segment={worldLen:F3}m (local {bone.localPosition.magnitude:F3}, bakeScale {bakeScale:F3}) — " +
                    "본 길이가 비정상 (Attach to Target으로 다시 저장 필요)");
            }
        }
    }

    static float GetBakeReferenceScale(Transform root)
    {
        var snapshot = root.GetComponent<HandPoseSnapshotRoot>();
        if (snapshot != null && snapshot.BakeReferenceScale > 0f)
            return snapshot.BakeReferenceScale;
        return 1f;
    }

    // 씬 인스턴스(handRoot.parent 있음): world 거리. 프리팹 에셋: local * bakeReferenceScale.
    static float EstimateWorldDistance(Transform handRoot, Transform from, Transform to, float bakeReferenceScale)
    {
        if (handRoot.parent != null)
            return Vector3.Distance(from.position, to.position);

        if (from == handRoot)
            return to.localPosition.magnitude * bakeReferenceScale;

        return to.localPosition.magnitude * bakeReferenceScale;
    }

    static Transform FindBone(Transform root, string boneName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == boneName)
                return t;
        }

        return null;
    }

    static bool IsNearZero(Vector3 v)
        => v.sqrMagnitude <= k_PosEpsilon * k_PosEpsilon;

    static bool IsNearIdentity(Quaternion q)
    {
        if (Quaternion.Angle(q, Quaternion.identity) <= k_RotEpsilon)
            return true;
        return false;
    }
}
