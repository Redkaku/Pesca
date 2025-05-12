using UnityEngine;

[System.Serializable]
public class FishAttributes
{
    // Para nivel Color
    public Color color;

    // Para nivel Especie / ColorYEspecie
    public GameObject speciesPrefab;

    // Para nivel Letra
    public char letter;

    // Para nivel Número
    public int number;

    // Para nivel Figura / FiguraYColor
    public Sprite shapeSprite;
    public Color shapeColor; // para FiguraYColor

    // Constructor de conveniencia (opcional)
    public FishAttributes(
        Color color = default,
        GameObject speciesPrefab = null,
        char letter = '\0',
        int number = 0,
        Sprite shapeSprite = null,
        Color shapeColor = default
    ) {
        this.color = color;
        this.speciesPrefab = speciesPrefab;
        this.letter = letter;
        this.number = number;
        this.shapeSprite = shapeSprite;
        this.shapeColor = shapeColor;
    }
}
