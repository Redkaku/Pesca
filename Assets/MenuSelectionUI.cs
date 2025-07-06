using UnityEngine;
using UnityEngine.UI;
using System;

using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

public class MenuSelectionUI : MonoBehaviour
{
    [Header("Nivel")]
    public Button[] levelButtons;         // 3 botones
    public Color selectedLevelColor = Color.yellow;
    public Color normalLevelColor   = Color.white;
    private int currentLevelIndex = 0;    // inicia en 0

    [Header("Modo")]
    public Button modeButton;
    public Sprite touchSprite;
    public Sprite hookSprite;
    private bool isTouchMode = true;

    [Header("Criterio")]
    public TMP_Dropdown criterionDropdown;    // valores de FishSpawner.Criterion

    [Header("Toggle Colores")]
    public Toggle colorToggle;            // “Usar todos los colores”

    [Header("Vista Previa")]
    public Image previewImage;            // muestra sprite/GIF por criterio
    public Sprite previewColor;
    public Sprite previewSpecies;
    public Sprite previewColorAndSpecies;
    public Sprite previewLetter;
    public Sprite previewNumber;
    public Sprite previewShape;
    public Sprite previewShapeAndColor;

    [Header("Empezar")]
    public Button startButton;

    // Evento para notificar (opcional) al GameManager o LevelLoader
    public Action<int, bool, FishSpawner.Criterion, bool> OnStart;

    private void Awake()
    {
        // 1) Inicializa nivel por defecto
        for(int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i;
            levelButtons[i].onClick.AddListener(() => SelectLevel(idx));
        }
        SelectLevel(0);

        // 2) Modo
        modeButton.onClick.AddListener(ToggleMode);
        UpdateModeButton();

        // 3) Llenar dropdown con los nombres del enum
        var names = Enum.GetNames(typeof(FishSpawner.Criterion)).ToList();
        criterionDropdown.ClearOptions();
        criterionDropdown.AddOptions(names);
        criterionDropdown.onValueChanged.AddListener(_ => {
            UpdateColorToggleVisibility();
            UpdatePreview();
        });

        // 4) Toggle colores
        colorToggle.onValueChanged.AddListener(_ => { /* si quieres reaccionar */ });
        UpdateColorToggleVisibility();

        // 5) Preview
        UpdatePreview();

        // 6) Start
        startButton.onClick.AddListener(() =>
        {
            // 1) Guardamos en el contenedor estático:
            GameSettings.LevelIndex   = currentLevelIndex;
            GameSettings.CaptureMode  = isTouchMode
                                        ? FishSpawner.CaptureMode.Click
                                        : FishSpawner.CaptureMode.Hook;
            GameSettings.Criterion    = (FishSpawner.Criterion)criterionDropdown.value;
            GameSettings.UseAllColors = colorToggle.isOn;

            // 2) Cargamos la escena según el modo:
            string sceneName = isTouchMode
                ? "PescaTouch"    // el nombre exacto de tu escena Touch
                : "PescaRed"; // el nombre exacto de tu escena Hook
            SceneManager.LoadScene(sceneName);
        });
    }

    private void SelectLevel(int idx)
    {
        currentLevelIndex = idx;
        for (int i = 0; i < levelButtons.Length; i++)
{
    var image = levelButtons[i].GetComponent<Image>();
    if (image != null)
        image.color = (i == idx) ? selectedLevelColor : normalLevelColor;
}

    }

    private void ToggleMode()
    {
        isTouchMode = !isTouchMode;
        UpdateModeButton();
    }

    private void UpdateModeButton()
    {
        var img = modeButton.GetComponent<Image>();
        img.sprite = isTouchMode ? touchSprite : hookSprite;
    }

    private void UpdateColorToggleVisibility()
    {
        var crit = (FishSpawner.Criterion)criterionDropdown.value;
        // Sólo mostrar si NO es Color
        colorToggle.gameObject.SetActive(crit != FishSpawner.Criterion.Color);
    }

    private void UpdatePreview()
    {
        var crit = (FishSpawner.Criterion)criterionDropdown.value;
        switch ((FishSpawner.Criterion)crit)
        {
            case FishSpawner.Criterion.Color:
                previewImage.sprite = previewColor;
                break;
            case FishSpawner.Criterion.Species:
                previewImage.sprite = previewSpecies;
                break;
            case FishSpawner.Criterion.ColorAndSpecies:
                previewImage.sprite = previewColorAndSpecies;
                break;
            case FishSpawner.Criterion.Letter:
                previewImage.sprite = previewLetter;
                break;
            case FishSpawner.Criterion.Number:
                previewImage.sprite = previewNumber;
                break;
            case FishSpawner.Criterion.Shape:
                previewImage.sprite = previewShape;
                break;
            case FishSpawner.Criterion.ShapeAndColor:
                previewImage.sprite = previewShapeAndColor;
                break;
        }
    }
}
