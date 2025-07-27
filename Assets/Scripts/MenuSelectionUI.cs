using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using System.Linq;
using UnityEngine.SceneManagement;

public class MenuSelectionUI : MonoBehaviour
{
    [Header("Nivel")]
    public Button[] levelButtons;
    public Color   selectedLevelColor = Color.yellow;
    public Color   normalLevelColor   = Color.white;
    private int    currentLevelIndex  = 0;

    [Header("Modo")]
    public Button modeButton;
    public Sprite touchSprite;
    public Sprite hookSprite;
    private bool  isTouchMode = true;

    [Header("Presets de criterio")]
    public SimpleCriterionData[] criterionDataList;

    [Header("Dropdown de criterio")]
    public TMP_Dropdown criterionDropdown;

    [Header("Toggle Colores")]
    public Toggle colorToggle;

    [Header("Preview en vídeo")]
    public RawImage    videoPreviewImage;
    public VideoPlayer previewPlayer;

    

    [Header("Empezar")]
    public Button startButton;

    void Awake()
    {
        // 1) Configurar botones de nivel
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i;
            levelButtons[i].onClick.AddListener(() => SelectLevel(idx));
        }
        SelectLevel(0);

        // 2) Botón de modo
        modeButton.onClick.AddListener(ToggleMode);
        UpdateModeButton();

        // 3) Dropdown de criterios
    var names = criterionDataList.Select(cd => cd.criterionName).ToList();
    criterionDropdown.ClearOptions();
    criterionDropdown.AddOptions(names);
    criterionDropdown.onValueChanged.AddListener(OnCriterionChanged);

    // 4) Toggle colores
    colorToggle.onValueChanged.AddListener(_ => UpdatePreviewVideo(criterionDropdown.value));

OnCriterionChanged(criterionDropdown.value);


        UpdateColorToggleVisibility();

        

        // 6) Botón Empezar
        startButton.onClick.AddListener(() =>
        {
            GameSettings.LevelIndex     = currentLevelIndex;
            GameSettings.CaptureMode    = isTouchMode
                                            ? FishSpawner.CaptureMode.Click
                                            : FishSpawner.CaptureMode.Hook;
            GameSettings.CriterionIndex = criterionDropdown.value;
            GameSettings.UseAllColors   = colorToggle.isOn;

            // Guardamos también qué escena de pesca viene
            GameSettings.NextFishScene = isTouchMode ? "PescaTouch" : "PescaRed";
            SceneManager.LoadScene(GameSettings.NextFishScene);
        });
    }

    void OnCriterionChanged(int idx)
{
    UpdatePreviewVideo(idx); // delegamos en este nuevo método
    UpdateColorToggleVisibility(); // también refrescamos visibilidad
}

private void UpdatePreviewVideo(int idx)
{
    var data = criterionDataList[idx];

    // Selecciona el video según el toggle
    var clip = colorToggle.isOn ? data.previewColorVideo : data.previewMonoVideo;

    if (clip != null)
    {
        if (!videoPreviewImage.gameObject.activeSelf)
            videoPreviewImage.gameObject.SetActive(true);

        previewPlayer.clip = clip;
        previewPlayer.isLooping = true;
        previewPlayer.Play();
    }
    else
    {
        previewPlayer.Stop();
        videoPreviewImage.gameObject.SetActive(false); // ocultamos si no hay video
    }
}



    private void SelectLevel(int idx)
    {
        currentLevelIndex = idx;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            var img = levelButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = (i == idx) ? selectedLevelColor : normalLevelColor;
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
    var data = criterionDataList[criterionDropdown.value];
    // Mostramos el toggle en todos los criterios salvo cuando el nombre sea "Colores"
    bool showToggle = data.criterionName != "Colores";
    colorToggle.gameObject.SetActive(showToggle);
}

}
