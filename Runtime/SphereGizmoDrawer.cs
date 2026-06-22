using UnityEngine;

// 부착한 GameObject 위치에 구(Sphere) 기즈모를 그린다. 씬 뷰에서만 보이며 게임 화면/빌드에는 영향이 없다.
public class SphereGizmoDrawer : MonoBehaviour
{
    [Tooltip("구의 반지름.")]
    [SerializeField, Min(0f)] private float radius = 0.5f;

    [Tooltip("기준 위치에서의 오프셋(로컬 공간).")]
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Tooltip("기즈모 색상.")]
    [SerializeField] private Color color = new Color(0f, 1f, 0f, 0.5f);

    [Tooltip("켜면 채워진 구, 끄면 와이어프레임 구를 그린다.")]
    [SerializeField] private bool solid = false;

    [Tooltip("켜면 항상, 끄면 오브젝트가 선택됐을 때만 그린다.")]
    [SerializeField] private bool drawAlways = true;

    private void OnDrawGizmos()
    {
        if (drawAlways)
            DrawSphere();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawAlways)
            DrawSphere();
    }

    private void DrawSphere()
    {
        // 오브젝트의 위치/회전/스케일을 반영해 로컬 오프셋 위치에 구를 그린다.
        Gizmos.color = color;
        Matrix4x4 prevMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (solid)
            Gizmos.DrawSphere(offset, radius);
        else
            Gizmos.DrawWireSphere(offset, radius);

        Gizmos.matrix = prevMatrix;
    }
}
