using UnityEngine;
using UnityEngine.Video;

public enum LetterMode
{
    None,
    All,
    Vowels
}
public enum Criterion
{
    Letter,
    Vocales,
    Number1_10,
    AllLetters,
    CustomRange,
    ColorAndSpecies,

    Color,
    Species,
    Shape,
    ShapeAndColor,
    FormasG,
    Number1_20
}

public enum NumberMode
{
    None,
    OneTo10,
    OneTo20,
    CustomRange
}

[CreateAssetMenu(menuName = "Game/Simple Criterion Data")]
public class SimpleCriterionData : ScriptableObject
{
    [Header("Identificador")]
    public Criterion  criterion; 
    public string criterionName;


    [Header("Prefabs (con color)")]
    public GameObject[]        coloredPrefabs;

    [Header("Prefabs (monocromo)")]
    public GameObject[]        whitePrefabs;

    [Header("Letras")]
    public LetterMode          letterMode = LetterMode.None;
    [Tooltip("Solo se usa si letterMode == Custom (no en este ejemplo)")]
    public char[]              customLetters;

    [Header("Números")]
    public NumberMode          numberMode = NumberMode.None;
    [Tooltip("Solo si numberMode == CustomRange")]
    public int                 numberMin;
    [Tooltip("Solo si numberMode == CustomRange")]
    public int                 numberMax;

    [Header("Vista previa (UI)")]
public VideoClip previewColorVideo;    // Cuando el toggle de colores está activado
public VideoClip previewMonoVideo;     // Cuando el toggle está desactivado
}
