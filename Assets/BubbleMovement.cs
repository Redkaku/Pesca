// BubbleMovement.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BubbleMovement : MonoBehaviour
{
    [Tooltip("Velocidad de desplazamiento")]
    public float speed = 2f;

    [Range(0f, 0.2f)]
    [Tooltip("Margen en viewport para rebotar antes de llegar al borde")]
    public float borderMargin = 0.05f;

    private Vector2 direction;
    private Camera mainCam;
    private float objectZ;

    void Awake()
    {
        mainCam   = Camera.main;
        objectZ   = transform.position.z;
        direction = Random.insideUnitCircle.normalized;
    }

    void Update()
    {
        // Posición tentativa
        Vector3 newPos = transform.position + (Vector3)(direction * speed * Time.deltaTime);

        // Proyección a viewport
        Vector3 vp = mainCam.WorldToViewportPoint(new Vector3(newPos.x, newPos.y, objectZ));

        // Rebote horizontal
        if ((vp.x < borderMargin && direction.x < 0) ||
            (vp.x > 1f - borderMargin && direction.x > 0))
        {
            direction.x = -direction.x;
        }
        // Rebote vertical
        if ((vp.y < borderMargin && direction.y < 0) ||
            (vp.y > 1f - borderMargin && direction.y > 0))
        {
            direction.y = -direction.y;
        }

        // Aplicar movimiento final
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }
}
