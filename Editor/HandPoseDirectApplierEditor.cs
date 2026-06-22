using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HandPoseDirectApplier))]
public class HandPoseDirectApplierEditor : Editor
{
    const string k_BindFbxPath = "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Models/LeftHand.fbx";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var applier = (HandPoseDirectApplier)target;
        EditorGUILayout.Space();

        if (applier.pose == null)
            EditorGUILayout.HelpBox("적용할 HandPoseData를 'pose'에 할당하세요.", MessageType.Info);

        using (new EditorGUI.DisabledScope(applier.pose == null))
        {
            if (GUILayout.Button("Apply Pose", GUILayout.Height(32)))
            {
                Undo.RegisterFullObjectHierarchyUndo(applier.gameObject, "Apply Hand Pose");
                applier.ApplyPose();
                MarkDirty(applier);
            }
        }

        if (GUILayout.Button("Reset To Bind"))
        {
            Undo.RegisterFullObjectHierarchyUndo(applier.gameObject, "Reset Hand Pose");
            ResetToBind(applier);
            MarkDirty(applier);
        }
    }

    // LeftHand fbx의 바인드 로컬 회전으로 되돌린다.
    static void ResetToBind(HandPoseDirectApplier applier)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(k_BindFbxPath);
        if (fbx == null)
        {
            Debug.LogWarning($"[HandPoseDirectApplier] 바인드 기준 fbx를 찾지 못했습니다: {k_BindFbxPath}");
            return;
        }

        var bind = new Dictionary<string, Quaternion>();
        foreach (var t in fbx.GetComponentsInChildren<Transform>(true))
            bind[t.name] = t.localRotation;

        foreach (var t in applier.GetComponentsInChildren<Transform>(true))
            if (bind.TryGetValue(t.name, out var rot))
                t.localRotation = rot;
    }

    static void MarkDirty(HandPoseDirectApplier applier)
    {
        if (Application.isPlaying)
            return;

        EditorUtility.SetDirty(applier);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
    }
}
