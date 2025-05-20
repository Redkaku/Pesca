using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    public Image           targetImage;
    public TextMeshProUGUI targetLabel;    // aquí muestras colorName, letra o número
    public Slider          progressSlider;
    public TextMeshProUGUI progressText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetTarget(Sprite s, string label)
    {
        targetImage.sprite = s;
        targetImage.color  = Color.white;
        targetLabel.text   = label;
    }

    public void UpdateProgress(int current, int target)
    {
        progressSlider.maxValue = target;
        progressSlider.value    = current;
        progressText.text       = $"{current} / {target}";
    }
}
