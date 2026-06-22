using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRDemo.EditorTools
{
    /// <summary>
    /// 선택한 인터랙터(또는 컨트롤러)의 Attach Transform을 그 손의 손목(L_Wrist / R_Wrist)에 맞춘다.
    /// 잡을 오브젝트의 Attach(고스트 손목)와 인터랙터 Attach(라이브 손목)가 같은 '손목'을 기준으로 정렬되어,
    /// 잡았을 때 라이브 손이 고스트 핸드와 정확히 겹친다.
    /// </summary>
    public static class InteractorAttachToWristTool
    {
        const string k_AttachName = "InteractorAttach (Wrist)";

        [MenuItem("Tools/VR/Align Interactor Attach To Hand Wrist")]
        static void AlignFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog("Interactor Attach", "인터랙터(또는 그걸 가진 컨트롤러)를 먼저 선택하세요.", "확인");
                return;
            }

            var interactor = go.GetComponent<XRBaseInteractor>() ?? go.GetComponentInChildren<XRBaseInteractor>(true);
            if (interactor == null)
            {
                EditorUtility.DisplayDialog("Interactor Attach", $"'{go.name}'에서 XRBaseInteractor를 찾지 못했습니다.", "확인");
                return;
            }

            var wrist = FindHandWrist(interactor.transform);
            if (wrist == null)
            {
                EditorUtility.DisplayDialog("Interactor Attach",
                    "손목(L_Wrist / R_Wrist)을 컨트롤러 하위에서 찾지 못했습니다.\n손 모델이 이 컨트롤러 하위에 있는지 확인하세요.", "확인");
                return;
            }

            var it = interactor.transform;
            Transform attach = it.Find(k_AttachName);
            if (attach == null)
            {
                var attachGo = new GameObject(k_AttachName);
                Undo.RegisterCreatedObjectUndo(attachGo, "Create Interactor Attach");
                attachGo.transform.SetParent(it, false);
                attach = attachGo.transform;
            }
            else
            {
                Undo.RecordObject(attach, "Update Interactor Attach");
            }

            // 손목의 월드 pose로 배치
            attach.SetPositionAndRotation(wrist.position, wrist.rotation);

            // 1) 기본 Attach Transform (직접/근접 잡기 등에서 사용)
            var so = new SerializedObject(interactor);
            var prop = so.FindProperty("m_AttachTransform");
            if (prop != null)
            {
                prop.objectReferenceValue = attach;
                so.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(interactor);

            // 2) NearFarInteractor 등: 실제 attach 기준은 InteractionAttachController의 'Transform To Follow'
            bool setFollow = false;
            var attachController = interactor.GetComponents<Component>()
                .FirstOrDefault(c => c != null && c.GetType().Name == "InteractionAttachController");
            if (attachController != null)
            {
                var so2 = new SerializedObject(attachController);
                var follow = so2.FindProperty("m_TransformToFollow");
                if (follow != null)
                {
                    follow.objectReferenceValue = attach;
                    so2.ApplyModifiedProperties();
                    EditorUtility.SetDirty(attachController);
                    setFollow = true;
                }
            }

            Selection.activeObject = attach.gameObject;
            Debug.Log($"[InteractorAttachToWrist] '{interactor.name}' Attach를 '{wrist.name}'에 맞췄습니다. " +
                      $"(AttachTransform 설정, Transform To Follow {(setFollow ? "설정됨" : "없음")})", attach);
        }

        // 인터랙터에서 위로 올라가며(컨트롤러까지) 손목 본을 찾는다.
        static Transform FindHandWrist(Transform interactorTransform)
        {
            for (Transform t = interactorTransform; t != null; t = t.parent)
            {
                var w = t.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(x => x.name == "L_Wrist" || x.name == "R_Wrist");
                if (w != null) return w;
            }
            return null;
        }
    }
}
