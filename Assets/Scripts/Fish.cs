using UnityEngine;
using System;
using TMPro;
public enum Species
{
    PezGlobo,
    Pulpo,
    Tiburon,
    Mantarraya,
    EstrellaMar,
    Cangrejo,
    Caballito,
    Ballena,
    Circulo,
    Triangulo,
    Estrella,
    Cuadrado,
    Rectangulo,
    Corazon,
    Flecha,
    Color,
    Hexagono,
    Pentagono,
    Trapecio,
    Rayo,
    Rombo
}




[RequireComponent(typeof(Collider2D))]
public class Fish : MonoBehaviour
{
    // Eventos
    public event Action<Fish> OnExitedScreen;
    public event Action<Fish> OnClicked;

    [Header("Identificación (dropdown)")]
    public Species   species;     // dropdown: PezGlobo, Pulpo, Mantarraya
    public FishColor colorName;   // dropdown: Rojo, Verde, Azul
    public char letter;      // '\0' si no aplica
public int? number;      // null si no aplica; 0..9 si aplica


    [Header("Referencias visuales")]
    public SpriteRenderer    bodyRenderer;
    public SpriteRenderer    shapeRenderer;   // figura interna
    public TextMeshPro letterText;
public TextMeshPro numberText;
    

    void Start()
{
    // Asumimos letterText y numberText refieren al mismo TextMeshPro o usas uno genérico.
    if (letterText != null) {
        if (letter != '\0') {
            letterText.text = letter.ToString();
        }
        else if (number.HasValue) {
            letterText.text = number.Value.ToString();
        }
        else {
            letterText.text = "";
        }
        // Mantén el objeto activo; si quieres ocultar visualmente cuando vacío, 
        // puedes ajustar alpha o dejar texto vacío.
    }
    if (shapeRenderer != null)
        shapeRenderer.gameObject.SetActive(shapeRenderer.sprite != null);
}



    void Update()
    {
        // El spawner implementa el movimiento
    }

    void OnMouseDown()
    {
        OnClicked?.Invoke(this);
    }

    /// <summary>
    /// Comprueba si salió por la derecha de la cámara.
    /// </summary>
    public void CheckOffScreen()
    {
        float rb = Camera.main.ViewportToWorldPoint(Vector3.right).x + 1f;
        if (transform.position.x > rb)
            OnExitedScreen?.Invoke(this);
    }
}
