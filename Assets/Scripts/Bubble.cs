using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Bubble : MonoBehaviour
{
    public UnityEvent<Bubble> OnPopped = new UnityEvent<Bubble>();

    [Header("Partes de la burbuja")]
    public SpriteRenderer mainBubbleRenderer;  // El sprite principal de la burbuja
    public GameObject     spriteContainer;     // Hijo que contiene sprites dinámicos
    public GameObject     popEffect;           // Hijo inicialmente desactivado

    [Header("Física tras estallar")]
    public float gravityScale = 1f;

    [Header("Autodestrucción")]
    [Tooltip("Segundos tras el pop antes de destruir la burbuja")]
    public float destroyDelay = 2f;

    private Collider2D  _collider;
    private Rigidbody2D _rb;
    private bool        _popped = false;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _rb       = GetComponent<Rigidbody2D>();
        if (_rb != null)
            _rb.isKinematic = true; // Sin física hasta estallar

        if (mainBubbleRenderer == null)
            mainBubbleRenderer = GetComponentInChildren<SpriteRenderer>();

        popEffect.SetActive(false);
    }

    void OnMouseDown()
    {
        Pop();
    }

    public void Pop()
    {
        if (_popped) return;
        _popped = true;

        // 1) Desactivar collider
        _collider.enabled = false;

        // 2) Ocultar sprite principal
        if (mainBubbleRenderer != null)
            mainBubbleRenderer.enabled = false;

        // 3) Activar efecto de pop (animación, partícula…)
        popEffect.SetActive(true);

        // 4) Habilitar gravedad para que caiga
        if (_rb != null)
        {
            _rb.isKinematic  = false;
            _rb.gravityScale = gravityScale;
        }

        // 5) Avisar a quien escuche
        OnPopped.Invoke(this);

        // 6) Programar destrucción tras unos segundos
        Destroy(gameObject, destroyDelay);
    }

    // Ya no necesitamos OnBecameInvisible: la burbuja se autodestruye
}
