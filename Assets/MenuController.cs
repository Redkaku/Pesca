using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Botones Principales")]
    public Button changeSceneButton;   // Carga otra escena
    public Button optionsButton;       // Abre panel de Opciones
    public Button creditsButton;       // Abre panel de Créditos
    public Button exitButton;          // Cierra la aplicación

    [Header("Paneles")]
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public Toggle specialEffectsToggle;
    [Header("Opciones > Volumen")]
    public Slider volumeSlider;
    public TMP_Text volumeLabel;


    [Header("Botones de Cierre de Paneles")]
    public Button closeOptionsButton;
    public Button closeCreditsButton;

    [Header("Configuración")]
    public string sceneToLoad = "OtraEscena";

    // —— NUEVO: Elementos UI de Opciones ——
    [Header("Opciones > Dificultad")]
    public Slider difficultySlider;      // Rango 0.10 → 0.70
    public TMP_Text difficultyLabel;       // "Fácil (0.65)"
    public Image difficultyIcon;        // Cambia sprite según franja
    public Sprite easySprite;
    public Sprite normalSprite;
    public Sprite hardSprite;

    void Start()
    {
        // Paneles cerrados al inicio
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        float savedVol = PlayerPrefs.GetFloat("masterVolume", 1f);
        AudioListener.volume = savedVol;

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = savedVol;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        UpdateVolumeLabel(savedVol);

        // Listeners básicos
        changeSceneButton.onClick.AddListener(OnChangeScene);
        optionsButton.onClick.AddListener(OnOpenOptions);
        creditsButton.onClick.AddListener(OnOpenCredits);
        exitButton.onClick.AddListener(OnExit);
        closeOptionsButton.onClick.AddListener(OnCloseOptions);
        closeCreditsButton.onClick.AddListener(OnCloseCredits);

        // —— Inicializamos el slider de dificultad ——
        // Slider normalizado de 0 (izq) a 1 (der)
        difficultySlider.minValue = 0f;
        difficultySlider.maxValue = 1f;

        // Lo inicializamos en el valor guardado
        float saved = GameSettings.FishSpawnProbability; // entre 0.10 y 0.70
                                                         // Convertimos saved → t en [0,1]:
        float t = Mathf.InverseLerp(0.70f, 0.10f, saved);
        difficultySlider.value = t;

        difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
        OnDifficultyChanged(t);
        specialEffectsToggle.isOn = GameSettings.SpecialEffectsEnabled;
        specialEffectsToggle.onValueChanged.AddListener(on => GameSettings.SpecialEffectsEnabled = on);
    }
    void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("masterVolume", v);
        UpdateVolumeLabel(v);
    }

    void UpdateVolumeLabel(float v)
    {
        if (volumeLabel != null)
            volumeLabel.text = $"Volumen: {(v * 100f):F0}%";
    }


    // 1) Cambiar de escena
    void OnChangeScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    // 2) Abrir panel de Opciones
    void OnOpenOptions()
    {
        optionsPanel.SetActive(true);
        // Podrías pausar audio/juego aquí si quieres
    }

    // 3) Abrir panel de Créditos
    void OnOpenCredits()
    {
        creditsPanel.SetActive(true);
    }

    // 4) Salir
    void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Cerrar paneles
    void OnCloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    void OnCloseCredits()
    {
        creditsPanel.SetActive(false);
    }

    // —— Lógica de Dificultad ——
    void OnDifficultyChanged(float t)
    {
        // Mapear t→probabilidad
        float prob = Mathf.Lerp(0.70f, 0.10f, t);
        GameSettings.FishSpawnProbability = prob;

        // Etiqueta y sprite
        string label;
        Sprite icon;
        if (prob >= 0.55f)
        {
            label = "Fácil";
            icon = easySprite;
        }
        else if (prob >= 0.30f)
        {
            label = "Normal";
            icon = normalSprite;
        }
        else
        {
            label = "Difícil";
            icon = hardSprite;
        }

        difficultyLabel.text = $"{label} ({prob * 100f:F0}%)";
        difficultyIcon.sprite = icon;
    }
}
