using UnityEngine;
using UnityEngine.UI;
using TMPro;                
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    [Header("Recuadro de Criterio")]
    public Image iconImage;             // Icono o recuadro de color
    public TextMeshProUGUI criterionText;  // Texto con TMP

    // Si quisieras mostrar, por ejemplo, todos los valores posibles:
    [Header("Opcional: Mostrar Paleta Completa")]
    public Transform paletteContainer;      // Un contenedor de UI para iconos
    public GameObject paletteItemPrefab;    // Un prefab con Image+TMP para cada valor

    private LevelManager lvl => LevelManager.Instance;
    private CriterionConfig cfg => lvl.currentConfig;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Llamarás a uno de estos métodos desde FishGenerator.ChooseAndShowTarget()

    public void ShowCriterionColor(Color c)
    {
        iconImage.sprite = null;
        iconImage.color  = c;
        criterionText.text = "Captura peces de este color";
        // Si quisieras mostrar toda la paleta:
        // ShowFullPalette(cfg.colores, col => iconImage.color = col, col => col);
    }

    public void ShowCriterionSpecies(Sprite speciesSprite)
    {
        iconImage.sprite = speciesSprite;
        iconImage.color  = Color.white;
        criterionText.text = "Captura esta especie";
    }

    public void ShowCriterionColorAndSpecies(Color c, Sprite speciesSprite)
    {
        iconImage.sprite = speciesSprite;
        iconImage.color  = c;
        criterionText.text = "Captura esta especie y color";
    }

    public void ShowCriterionLetter(char letter)
    {
        iconImage.sprite = null;
        iconImage.color  = Color.white;
        criterionText.text = $"Captura peces con letra “{letter}”";
    }

    public void ShowCriterionNumber(int number)
    {
        iconImage.sprite = null;
        iconImage.color  = Color.white;
        criterionText.text = $"Captura peces con número {number}";
    }

    public void ShowCriterionShape(Sprite shapeSprite)
    {
        iconImage.sprite = shapeSprite;
        iconImage.color  = Color.white;
        criterionText.text = "Captura peces con esta figura";
    }

    public void ShowCriterionShapeAndColor(Sprite shapeSprite, Color c)
    {
        iconImage.sprite = shapeSprite;
        iconImage.color  = c;
        criterionText.text = "Captura esta figura y color";
    }

    // Ejemplo genérico de cómo podrías mostrar la paleta completa:
    private void ShowFullPalette<T>(List<T> items, 
                                    System.Action<T> setupIcon, 
                                    System.Func<T,string> toLabel)
    {
        // Limpia viejos items
        foreach (Transform child in paletteContainer)
            Destroy(child.gameObject);

        // Crea uno por valor en cfg
        foreach (var item in items)
        {
            var go = Instantiate(paletteItemPrefab, paletteContainer);
            var img = go.GetComponentInChildren<Image>();
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();

            // Configura icono/etiqueta según tipo T
            if (item is Color col)      img.color = col;
            else if (item is Sprite sp) img.sprite = sp;

            txt.text = toLabel(item);
        }
    }
}
