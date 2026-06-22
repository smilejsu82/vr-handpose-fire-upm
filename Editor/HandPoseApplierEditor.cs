using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HandPoseApplier))]
public class HandPoseApplierEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var applier = (HandPoseApplier)target;
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
            applier.ResetToBind();
            MarkDirty(applier);
        }
    }

    static void MarkDirty(HandPoseApplier applier)
    {
        if (Application.isPlaying)
            return;

        EditorUtility.SetDirty(applier);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(applier.gameObject.scene);
    }
}
