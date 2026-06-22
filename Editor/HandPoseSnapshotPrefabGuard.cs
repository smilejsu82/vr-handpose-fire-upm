using UnityEditor;
using UnityEngine;

// HandPose 프리팹 저장·임포트 시 좌표계가 깨진 에셋을 경고한다.
public class HandPoseSnapshotPrefabGuard : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (!path.EndsWith(".prefab") || path.IndexOf("HandPoses/Prefabs", System.StringComparison.Ordinal) < 0)
                continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<HandPoseSnapshotRoot>() == null)
                continue;

            var result = HandPoseSnapshotValidator.Validate(prefab);
            if (!result.Valid)
                Debug.LogError($"[HandPoseSnapshot] 프리팹 검증 실패: {path}\n{result.Message}", prefab);
        }
    }
}

[CustomEditor(typeof(HandPoseSnapshotRoot))]
public class HandPoseSnapshotRootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var root = (HandPoseSnapshotRoot)target;
        var result = HandPoseSnapshotValidator.Validate(root.gameObject, root.transform.parent);

        EditorGUILayout.Space();
        string baked = string.IsNullOrEmpty(root.BakedVersion) ? "(미상 / 옛 버전)" : root.BakedVersion;
        EditorGUILayout.LabelField("Baked Version", $"{baked}  (현재 도구 v{HandPoseRecorderWindow.Version})");
        if (root.BakedVersion != HandPoseRecorderWindow.Version)
            EditorGUILayout.HelpBox(
                $"이 스냅샷은 현재 도구(v{HandPoseRecorderWindow.Version})와 다른 버전으로 만들어졌습니다.\n" +
                "위치가 어긋난다면 옛 좌표 버그가 포함됐을 수 있으니, 손을 다시 잡고 Attach to Target으로 새로 저장하세요.",
                MessageType.Warning);
        EditorGUILayout.LabelField("Bake Reference Scale", root.BakeReferenceScale.ToString("F3"));
        EditorGUILayout.LabelField("Mesh Frozen", root.MeshFrozen ? "Yes" : "No");
        if (result.Valid)
            EditorGUILayout.HelpBox(result.Message, MessageType.Info);
        else
            EditorGUILayout.HelpBox(result.Message, MessageType.Error);

        if (GUILayout.Button("Snap To Parent Origin"))
        {
            Undo.RecordObject(root.transform, "Snap Hand Pose Snapshot");
            root.SnapToParentOrigin();
            EditorUtility.SetDirty(root);
        }
    }
}
