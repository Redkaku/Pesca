using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Objetivo")]
    public Image targetImage;            // recuadro o icono
    public TextMeshProUGUI targetLabel;  // texto para letra/número o nombre

    [Header("Estilo Texto Objetivo")]
    public Vector2 textAnchoredPos = Vector2.zero;
    public float   textFontSize = 24;
    public Color   textColor    = Color.black;

    [Header("Estilo Texto Letter/Number")]
    public Vector2 textLNAnchoredPos = Vector2.zero;
    public float   textLNFontSize = 48;
    public Color   textLNColor    = Color.black;

    [Header("Progreso")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        progressSlider.interactable = false;

        // Y para ignorar por completo los clics en el handle:
        if (progressSlider.handleRect != null)
        {
            var handleImg = progressSlider.handleRect.GetComponent<Image>();
            if (handleImg != null)
                handleImg.raycastTarget = false;
        }

    
    }

    /// <summary>
    /// Muestra un objetivo basado en sprite. Si label no es null/empty, también lo muestra.
    /// </summary>
    /// <param name="sprite">Sprite a mostrar en targetImage (puede ser null si no hay icono).</param>
    /// <param name="label">Texto a mostrar (puede ser null o vacío para ocultar).</param>
    /// <param name="useLNStyle">Si es true, aplica estilo Letter/Number; si false, estilo por defecto sprite.</param>
    public void ShowSpriteTarget(Sprite sprite, string label = null, bool useLNStyle = false)
    {
        // Mostrar recuadro con sprite
        targetImage.gameObject.SetActive(sprite != null);
        targetImage.sprite = sprite;
        targetImage.color = Color.white; // sin tint adicional por defecto aquí

        // Mostrar u ocultar texto
        if (!string.IsNullOrEmpty(label))
        {
            targetLabel.gameObject.SetActive(true);
            targetLabel.text = label;

            // Ajustar estilo: si es letra/número y quieres estilo más grande, pasas useLNStyle=true
            var rt = targetLabel.rectTransform;
            if (useLNStyle)
            {
                rt.anchoredPosition = textLNAnchoredPos;
                targetLabel.fontSize = textLNFontSize;
                targetLabel.color = textLNColor;
            }
            else
            {
                rt.anchoredPosition = textAnchoredPos;
                targetLabel.fontSize = textFontSize;
                targetLabel.color = textColor;
            }
        }
        else
        {
            targetLabel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Muestra un objetivo solo texto (letra o número). Oculta el recuadro.
    /// </summary>
    public void ShowTextTarget(string text)
    {
        targetImage.gameObject.SetActive(false);

        targetLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
        targetLabel.text = text;
        var rt = targetLabel.rectTransform;
        rt.anchoredPosition = textLNAnchoredPos;
        targetLabel.fontSize = textLNFontSize;
        targetLabel.color = textLNColor;
    }

    /// <summary>
/// Mostrar objetivo de tipo Color: colorea el recuadro y muestra el nombre del color.
/// </summary>
public void ShowColorTarget(Color color, string label = null)
{
    // Mostrar recuadro coloreado
    targetImage.gameObject.SetActive(true);
    targetImage.sprite = null;
    targetImage.color = color;

    // Mostrar texto con el nombre del color
    if (!string.IsNullOrEmpty(label))
    {
        targetLabel.gameObject.SetActive(true);
        targetLabel.text = label;
        // Usa estilo por defecto o de Letter/Number según prefieras; aquí asumimos estilo por defecto:
        var rt = targetLabel.rectTransform;
        rt.anchoredPosition = textAnchoredPos;
        targetLabel.fontSize = textFontSize;
        targetLabel.color = textColor;
    }
    else
    {
        targetLabel.gameObject.SetActive(false);
    }
}

    public void UpdateProgress(int current, int target)
{
    if (progressSlider != null)
    {
        progressSlider.maxValue     = target;
        progressSlider.value        = current;
        progressSlider.interactable = false;  // <— impide que el usuario lo mueva
    }
    if (progressText != null)
        progressText.text = $"{current} / {target}";
}
}
