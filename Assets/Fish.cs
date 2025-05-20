using UnityEngine;
using System;
using TMPro;
public enum Species
{
    PezGlobo,
    Pulpo,
    Mantarraya
}

public enum FishColor
{
    Rojo,
    Verde,
    Azul
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
    public char      letter;      // p.ej. 'A'
    public int       number;      // p.ej. 5

    [Header("Referencias visuales")]
    public SpriteRenderer    bodyRenderer;
    public SpriteRenderer    shapeRenderer;   // figura interna
    public TextMeshProUGUI   letterText;      // letra impresa
    public TextMeshProUGUI   numberText;      // número impreso

    void Start()
    {
        // Ajusta letra
        if (letterText != null)
        {
            bool hasLetter = letter != '\0';
            letterText.text = hasLetter ? letter.ToString() : "";
            letterText.gameObject.SetActive(hasLetter);
        }

        // Ajusta número
        if (numberText != null)
        {
            bool hasNumber = number != 0;
            numberText.text = hasNumber ? number.ToString() : "";
            numberText.gameObject.SetActive(hasNumber);
        }

        // Ajusta figura interna
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
