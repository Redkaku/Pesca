using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishSpawner : MonoBehaviour
{
    public enum Criterion
    {
        Color,
        Species,
        ColorAndSpecies,
        Letter,
        Number,
        Shape,
        ShapeAndColor
    }

    [Header("Criterio de nivel (selecciona sólo uno)")]
    public Criterion currentCriterion;

    [Header("Prefabs de peces")]
    public List<GameObject> fishPrefabs; 

    [Header("Sprites de figura (Shape)")]
    public List<Sprite> shapeSprites;

    [Header("Spawn y movimiento")]
    public float spawnInterval = 0.5f;
    public float speed         = 2f;
    public float amplitude     = 1f;
    public float frequency     = 2f;

    [Header("Meta de capturas")]
    public int targetCount = 10;

    // Objetivo actual
    private Species   targetSpecies;
    private FishColor targetColor;
    private char      targetLetter;
    private int       targetNumber;
    private Sprite    targetShape;

    private int caughtCount;
    private List<Fish> activeFish = new List<Fish>();

    // Listas dinámicas
    private List<Species>   availableSpecies;
    private List<FishColor> availableColors;

    void Start()
    {
        // *** Construir listas únicas de especies y colores ***
        var speciesSet = new HashSet<Species>();
        var colorSet   = new HashSet<FishColor>();
        foreach (var prefab in fishPrefabs)
        {
            var f = prefab.GetComponent<Fish>();
            if (f != null)
            {
                speciesSet.Add(f.species);
                colorSet.Add(f.colorName);
            }
        }
        availableSpecies = new List<Species>(speciesSet);
        availableColors  = new List<FishColor>(colorSet);

        PickNewTarget();
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnFish();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnFish()
    {
        var prefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        var go     = Instantiate(prefab, GetSpawnPosition(), Quaternion.identity);
        var fish   = go.GetComponent<Fish>();
        if (fish == null) return;

        // Sólo asignar letra/número/figura si el criterio lo pide
        switch (currentCriterion)
        {
            case Criterion.Letter:
                fish.letter = (char)('A' + Random.Range(0, 26));
                break;
            case Criterion.Number:
                fish.number = Random.Range(0, 10);
                break;
            case Criterion.Shape:
            case Criterion.ShapeAndColor:
                fish.shapeRenderer.sprite = shapeSprites[
                    Random.Range(0, shapeSprites.Count)
                ];
                break;
        }

        // Suscribir eventos
        fish.OnExitedScreen += OnFishExited;
        fish.OnClicked      += OnFishClicked;
        activeFish.Add(fish);
    }

    void Update()
    {
        for (int i = activeFish.Count - 1; i >= 0; i--)
        {
            var f = activeFish[i];
            float x = f.transform.position.x + speed * Time.deltaTime;
            float y = f.transform.position.y 
                      + Mathf.Sin(Time.time * frequency) * amplitude * Time.deltaTime;
            f.transform.position = new Vector2(x, y);
            f.CheckOffScreen();
        }
    }

    void OnFishExited(Fish f)
    {
        activeFish.Remove(f);
        Destroy(f.gameObject);
    }

    void OnFishClicked(Fish f)
    {
        bool correct = false;

        switch (currentCriterion)
        {
            case Criterion.Color:
                correct = (f.colorName == targetColor);
                break;
            case Criterion.Species:
                correct = (f.species == targetSpecies);
                break;
            case Criterion.ColorAndSpecies:
                correct = f.species == targetSpecies
                       && f.colorName == targetColor;
                break;
            case Criterion.Letter:
                correct = (f.letter == targetLetter);
                break;
            case Criterion.Number:
                correct = (f.number == targetNumber);
                break;
            case Criterion.Shape:
                correct = (f.shapeRenderer.sprite == targetShape);
                break;
            case Criterion.ShapeAndColor:
                correct = f.shapeRenderer.sprite == targetShape
                       && f.colorName == targetColor;
                       
                break;
        }

        if (correct)
        {
            caughtCount++;
            UIController.Instance.UpdateProgress(caughtCount, targetCount);
            if (caughtCount >= targetCount)
                PickNewTarget();
        }

        activeFish.Remove(f);
        Destroy(f.gameObject);
    }

    Vector3 GetSpawnPosition()
    {
        var cam = Camera.main;
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        return new Vector3(cam.transform.position.x - w - 1f,
                           Random.Range(cam.transform.position.y - h,
                                         cam.transform.position.y + h),
                           0f);
    }

    void PickNewTarget()
    {
        caughtCount = 0;

        // Elegir especie y color de las listas únicas
        targetSpecies = availableSpecies[
            Random.Range(0, availableSpecies.Count)
        ];
        targetColor   = availableColors[
            Random.Range(0, availableColors.Count)
        ];

        // Letra, número y figura si toca
        targetLetter = (currentCriterion == Criterion.Letter)
            ? (char)('A' + Random.Range(0, 26))
            : '\0';
        targetNumber = (currentCriterion == Criterion.Number)
            ? Random.Range(0, 10)
            : 0;
        targetShape = (currentCriterion == Criterion.Shape
                    || currentCriterion == Criterion.ShapeAndColor)
            ? shapeSprites[Random.Range(0, shapeSprites.Count)]
            : null;

        // Actualizar UI
        Sprite iconSprite = null;
        string label      = "";

        switch (currentCriterion)
        {
            case Criterion.Color:
                iconSprite = fishPrefabs.Find(p =>
                    p.GetComponent<Fish>().colorName == targetColor
                ).GetComponent<SpriteRenderer>().sprite;
                label      = targetColor.ToString();
                break;
            case Criterion.Species:
                iconSprite = fishPrefabs.Find(p =>
                    p.GetComponent<Fish>().species == targetSpecies
                ).GetComponent<SpriteRenderer>().sprite;
                label      = targetSpecies.ToString();
                break;
            case Criterion.ColorAndSpecies:
                var pf = fishPrefabs.Find(p => {
                    var f = p.GetComponent<Fish>();
                    return f.species == targetSpecies
                        && f.colorName == targetColor;
                });
                iconSprite = pf.GetComponent<SpriteRenderer>().sprite;
                label      = $"{targetColor} {targetSpecies}";
                break;
            case Criterion.Letter:
                label = targetLetter.ToString();
                break;
            case Criterion.Number:
                label = targetNumber.ToString();
                break;
            case Criterion.Shape:
                iconSprite = targetShape;
                label      = "Figura";
                break;
            case Criterion.ShapeAndColor:
                iconSprite = targetShape;
                label      = $"{targetColor} Figura";
                break;
        }

        UIController.Instance.SetTarget(iconSprite, label);
        UIController.Instance.UpdateProgress(0, targetCount);
    }
}
