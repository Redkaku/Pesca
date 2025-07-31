using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Objetivo con Sprite/Color")]
    public Image                  targetImage;    // recuadro o icono
    public TextMeshProUGUI        defaultLabel;   // sólo para nombre de color o texto pequeño

    [Header("Objetivo Letter/Number/Text")]
    public TextMeshProUGUI        lnLabel;        // sólo para letras/números

    [Header("Progreso")]
    public Slider                 progressSlider;
    public TextMeshProUGUI        progressText;
    public TextMeshProUGUI labelText; // para letras/números
public TextMeshProUGUI nameText;  // para nombres o colores


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        progressSlider.interactable = false;
        if (progressSlider.handleRect != null)
        {
            var h = progressSlider.handleRect.GetComponent<Image>();
            if (h != null) h.raycastTarget = false;
        }
    }

    /// <summary> Muestra icono o cuadro coloreado y texto pequeño (nombre de color). </summary>
    public void ShowSpriteTarget(Sprite sprite, string label = null, Color? tint = null)
    {
        // sprite / color
        targetImage.gameObject.SetActive(true);
        targetImage.sprite = sprite;
        targetImage.color  = tint ?? Color.white;

        // defaultLabel
        defaultLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));
        defaultLabel.text = label ?? "";
        // lnLabel oculto
        lnLabel.gameObject.SetActive(false);
    }

    /// <summary> Muestra sólo texto grande (letra, número o cualquier texto). </summary>
    public void ShowTextTarget(string text, Color? color = null)
    {
        // oculta imagen y defaultLabel
        targetImage.gameObject.SetActive(false);
        defaultLabel.gameObject.SetActive(false);

        // muestra lnLabel
        lnLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
        lnLabel.text  = text ?? "";
        if (color.HasValue) lnLabel.color = color.Value;
    }
    

    public void UpdateProgress(int current, int target)
    {
        progressSlider.maxValue = target;
        progressSlider.value    = current;
        progressText.text       = $"{current} / {target}";
    }
}
