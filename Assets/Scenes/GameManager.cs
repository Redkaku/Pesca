using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;   

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Meta de Capturas")]
    [Tooltip("Número de capturas correctas necesarias para terminar el nivel")]
    public int targetCount = 10;

    [Header("Referencias UI")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    // Lleva la cuenta de capturas correctas
    private int currentCount = 0;

    // Evento público para fin de nivel
    public event Action OnLevelComplete;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

   private void Start()
{
    // Inicializar Slider
    progressSlider.maxValue = targetCount;
    progressSlider.value = 0;
    UpdateProgressText();

    // Suscribirse al evento público de FishGenerator
    FishGenerator generator = FindObjectOfType<FishGenerator>();
    if (generator != null)
        generator.OnFishCaught += HandleFishCaught;
}


    private void HandleFishCaught(Fish fish, bool wasCorrect)
    {
        if (wasCorrect)
        {
            currentCount++;
            progressSlider.value = currentCount;
            UpdateProgressText();

            if (currentCount >= targetCount)
                LevelCompleted();
        }
        else
        {
            // Aquí podrías restar tiempo, vidas, o simplemente dar feedback
        }
    }

    private void UpdateProgressText()
    {
        progressText.text = $"{currentCount} / {targetCount}";
    }

    private void LevelCompleted()
    {
        OnLevelComplete?.Invoke();
        Debug.Log("¡Nivel completado!");
        // Aquí puedes, por ejemplo, activar un objeto, cambiar de escena, etc.
    }
}
