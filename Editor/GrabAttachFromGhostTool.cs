using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRDemo.EditorTools
{
    /// <summary>
    /// 선택한 "잡을 오브젝트"의 고스트 핸드 손목(L_Wrist / R_Wrist) 위치에
    /// Attach Transform을 자동 생성하고 XRGrabInteractable에 연결한다.
    /// 그러면 잡을 때 오브젝트가 그 손목 지점을 인터랙터에 정렬해, 라이브 손이 고스트와 맞춰진다.
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
                EditorUtility.DisplayDialog("Grab Attach", "잡을 오브젝트(XRGrabInteractable이 있는 것)를 먼저 선택하세요.", "확인");
                return;
            }

            var grab = go.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                EditorUtility.DisplayDialog("Grab Attach", $"'{go.name}'에 XRGrabInteractable이 없습니다.", "확인");
                return;
            }

            var wrist = FindGhostWrist(go.transform);
            if (wrist == null)
            {
                EditorUtility.DisplayDialog("Grab Attach",
                    "고스트 손목(L_Wrist 또는 R_Wrist)을 오브젝트 하위에서 찾지 못했습니다.\n" +
                    "Attach to Target으로 만든 고스트 핸드가 이 오브젝트의 자식인지 확인하세요.", "확인");
                return;
            }

            // 기존 attach가 우리가 만든 것이면 재사용, 아니면 새로 생성
            Transform attach = go.transform.Find(k_AttachName);
            if (attach == null)
            {
                var attachGo = new GameObject(k_AttachName);
                Undo.RegisterCreatedObjectUndo(attachGo, "Create Grab Attach");
                attachGo.transform.SetParent(go.transform, false);
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
            Debug.Log($"[GrabAttachFromGhost] '{go.name}'에 Attach Transform 생성·연결 완료 (기준: {wrist.name}).", attach);
        }

        // 오브젝트 하위에서 고스트 손목 본을 찾는다. HandPoseSnapshotRoot 하위 우선, 없으면 이름으로.
        static Transform FindGhostWrist(Transform root)
        {
            var snapshotType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                .FirstOrDefault(t => t.Name == "HandPoseSnapshotRoot");

            // 1) 고스트 스냅샷 루트 하위의 손목
            if (snapshotType != null)
            {
                foreach (var comp in root.GetComponentsInChildren(snapshotType, true))
                {
                    var w = FindWristByName(((Component)comp).transform);
                    if (w != null) return w;
                }
            }

            // 2) 오브젝트 전체에서 손목 이름으로
            return FindWristByName(root);
        }

        static Transform FindWristByName(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "L_Wrist" || t.name == "R_Wrist");
        }
    }
}
