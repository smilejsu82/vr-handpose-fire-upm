using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRDemo.EditorTools
{
    /// <summary>
    /// 고스트 핸드(HandPose_Snapshot) 또는 잡을 오브젝트를 선택하고 실행하면,
    /// 고스트의 손목(L_Wrist / R_Wrist) 위치에 Attach Transform을 만들어
    /// 상위 XRGrabInteractable에 연결한다.
    /// 잡을 때 오브젝트가 그 손목 지점을 인터랙터에 정렬해, 라이브 손이 고스트와 맞춰진다.
    /// </summary>
    public static class GrabAttachFromGhostTool
    {
        const string k_AttachName = "GrabAttach (Ghost Wrist)";

        [MenuItem("Tools/VR/Create Grab Attach From Ghost Hand")]
        static void CreateFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog("Grab Attach",
                    "고스트 핸드(HandPose_Snapshot) 또는 잡을 오브젝트를 선택한 뒤 실행하세요.", "확인");
                return;
            }

            // 잡을 오브젝트 = 선택의 상위(또는 자신)에 있는 XRGrabInteractable
            var grab = go.GetComponentInParent<XRGrabInteractable>();
            if (grab == null)
            {
                EditorUtility.DisplayDialog("Grab Attach",
                    $"'{go.name}'의 상위에 XRGrabInteractable이 없습니다.\n고스트 핸드는 잡을 오브젝트의 자식이어야 합니다.", "확인");
                return;
            }

            // 손목 = 선택한 것(고스트) 하위 우선, 없으면 잡을 오브젝트 하위에서 검색
            var wrist = FindWristByName(go.transform) ?? FindWristByName(grab.transform);
            if (wrist == null)
            {
                EditorUtility.DisplayDialog("Grab Attach",
                    "고스트 손목(L_Wrist / R_Wrist)을 찾지 못했습니다.\nAttach to Target으로 만든 고스트 핸드를 선택했는지 확인하세요.", "확인");
                return;
            }

            // attach를 둘 부모 = 고스트 핸드(HandPoseSnapshotRoot) 자식. 없으면 손목 하위.
            var ghostRoot = go.GetComponentInParent<HandPoseSnapshotRoot>()
                            ?? go.GetComponentInChildren<HandPoseSnapshotRoot>(true)
                            ?? grab.GetComponentInChildren<HandPoseSnapshotRoot>(true);
            Transform parent = ghostRoot != null ? ghostRoot.transform : wrist;

            Transform attach = parent.Find(k_AttachName);
            if (attach == null)
            {
                var attachGo = new GameObject(k_AttachName);
                Undo.RegisterCreatedObjectUndo(attachGo, "Create Grab Attach");
                attachGo.transform.SetParent(parent, false);
                attach = attachGo.transform;
            }
            else
            {
                Undo.RecordObject(attach, "Update Grab Attach");
            }

            // 고스트 손목의 월드 pose로 배치
            attach.SetPositionAndRotation(wrist.position, wrist.rotation);

            // XRGrabInteractable.attachTransform 연결
            var so = new SerializedObject(grab);
            var prop = so.FindProperty("m_AttachTransform");
            if (prop != null)
            {
                prop.objectReferenceValue = attach;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(grab);
            Selection.activeObject = attach.gameObject;
            Debug.Log($"[GrabAttachFromGhost] '{grab.name}'에 Attach Transform 생성·연결 완료 (기준: {wrist.name}).", attach);
        }

        static Transform FindWristByName(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "L_Wrist" || t.name == "R_Wrist");
        }
    }
}
