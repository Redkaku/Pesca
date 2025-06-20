// Opcional: script para visualizar línea de superficie en el Editor
using UnityEngine;
[ExecuteInEditMode]
public class SurfaceMarkerGizmo : MonoBehaviour
{
    public Color gizmoColor = Color.cyan;
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        float y = transform.position.y;
        // Dibuja una línea horizontal larga:
        Gizmos.DrawLine(new Vector3(-100, y, 0), new Vector3(100, y, 0));
    }
}
