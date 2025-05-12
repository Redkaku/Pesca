using UnityEngine;
using System;
using TMPro;  

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Fish : MonoBehaviour
{
    // Eventos para notificar al generador
    public event Action<Fish> OnExitedScreen;
    public event Action<Fish, bool> OnCaughtCorrect;

    // Movimiento
    private float horizontalSpeed;
    private float sineAmp, sineFreq;
    private Vector2 startPos;
    private float rightBoundX;        // ← límite derecho pasado desde el generador

    // Atributos de este pez
    private FishAttributes attributes;

    // Referencias de componentes visuales
    [Header("Visual Components")]
    public SpriteRenderer bodyRenderer;   
    public SpriteRenderer shapeRenderer;  
    public TextMeshProUGUI letterText;           
    public TextMeshProUGUI numberText;           

    private void Update()
    {
        MoveFish();
        CheckOffScreen();
    }

    /// <summary>
    /// Inicializa velocidad, oscilación y el límite derecho de despawn.
    /// </summary>
    public void InitMovement(float speed, float amp, float freq)
{
    Debug.Log($"[Fish] InitMovement received speed={speed}, amp={amp}, freq={freq}");
    horizontalSpeed = speed;
    sineAmp        = amp;
    sineFreq       = freq;
    startPos       = transform.position;
}


    public void SetAttributes(FishAttributes attr)
    {
        attributes = attr;

        // 1) Color/Especie (bodyRenderer)
        bodyRenderer.color = attr.color;
        if (attr.speciesPrefab != null)
        {
            var sprite = attr.speciesPrefab
                               .GetComponent<SpriteRenderer>()
                               .sprite;
            bodyRenderer.sprite = sprite;
        }

        // 2) Texto de letra
        if (letterText != null)
        {
            bool hasLetter = attr.letter != '\0';
            letterText.gameObject.SetActive(hasLetter);
            if (hasLetter) letterText.text = attr.letter.ToString();
        }

        // 3) Texto de número
        if (numberText != null)
        {
            bool hasNumber = attr.number != 0;
            numberText.gameObject.SetActive(hasNumber);
            if (hasNumber) numberText.text = attr.number.ToString();
        }

        // 4) Figura interna
        if (shapeRenderer != null)
        {
            bool hasShape = attr.shapeSprite != null;
            shapeRenderer.gameObject.SetActive(hasShape);
            if (hasShape)
            {
                shapeRenderer.sprite = attr.shapeSprite;
                shapeRenderer.color  = attr.shapeColor;
            }
        }
    }

    private void MoveFish()
    {
        Debug.Log($"[Fish] MoveFish: pos={transform.position}, speed={horizontalSpeed}");
        transform.Translate(Vector2.right * horizontalSpeed * Time.deltaTime);
        float newY = startPos.y + Mathf.Sin(Time.time * sineFreq) * sineAmp;
        transform.position = new Vector2(transform.position.x, newY);
    }

    private void CheckOffScreen()
{
    float rightBound = Camera.main.ViewportToWorldPoint(Vector3.right).x + 1f;
    if (transform.position.x > rightBound)
        OnExitedScreen?.Invoke(this);
}


    private void OnMouseDown()
    {
        HandleCapture();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("FishingNet"))
            HandleCapture();
    }

    private void HandleCapture()
    {
        bool isCorrect = EvaluateMatch();
        OnCaughtCorrect?.Invoke(this, isCorrect);
    }
    
    private bool EvaluateMatch()
    {
        var lvl = LevelManager.Instance;
        var tgt = lvl.targetAttributes;

        switch (lvl.criterioSeleccionado)
        {
            case LevelManager.Criterio.Color:
                return attributes.color == tgt.color;

            case LevelManager.Criterio.Especie:
                return attributes.speciesPrefab == tgt.speciesPrefab;

            case LevelManager.Criterio.ColorYEspecie:
                return attributes.color == tgt.color
                    && attributes.speciesPrefab == tgt.speciesPrefab;

            case LevelManager.Criterio.Letra:
                return attributes.letter == tgt.letter;

            case LevelManager.Criterio.Numero:
                return attributes.number == tgt.number;

            case LevelManager.Criterio.Figura:
                return attributes.shapeSprite == tgt.shapeSprite;

            case LevelManager.Criterio.FiguraYColor:
                return attributes.shapeSprite == tgt.shapeSprite
                    && attributes.shapeColor == tgt.shapeColor;

            default:
                return false;
        }
    }
}
