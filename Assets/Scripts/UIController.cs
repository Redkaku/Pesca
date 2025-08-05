using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Objetivo Sprite")]
    public Image               targetImage;    // recuadro o icono
    public TextMeshProUGUI     spriteLabel;    // texto junto al icono

    [Header("Objetivo Texto")]
    public TextMeshProUGUI     textOnlyLabel;  // para letras, números o vocales

    [Header("Progreso")]
    public Slider              progressSlider;
    public TextMeshProUGUI     progressText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        progressSlider.interactable = false;
        if (progressSlider.handleRect != null)
            progressSlider.handleRect.GetComponent<Image>().raycastTarget = false;
    }

    public void ShowTextTarget(string text, Color color)
    {
        targetImage.gameObject.SetActive(false);
        spriteLabel.gameObject.SetActive(false);

        textOnlyLabel.gameObject.SetActive(true);
        textOnlyLabel.text  = text;
        textOnlyLabel.color = color;
    }

    public void ShowSpriteTarget(Sprite sprite, string label = null, Color? tint = null)
{
    textOnlyLabel.gameObject.SetActive(false);

    targetImage.gameObject.SetActive(sprite != null);
    targetImage.sprite = sprite;
    targetImage.color  = tint ?? Color.white;

    if (!string.IsNullOrEmpty(label))
    {
        spriteLabel.gameObject.SetActive(true);
        spriteLabel.text  = label;
        spriteLabel.color = Color.white;   // siempre blanco
    }
    else
    {
        spriteLabel.gameObject.SetActive(false);
    }
}


    public void UpdateProgress(int current, int target)
    {
        progressSlider.maxValue = target;
        progressSlider.value    = current;
        progressText.text       = $"{current} / {target}";
    }
}
