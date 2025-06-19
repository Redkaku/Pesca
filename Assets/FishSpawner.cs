using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FishSpawner : MonoBehaviour
{
    public enum Criterion { Color, Species, ColorAndSpecies, Letter, Number, Shape, ShapeAndColor }
    public enum CaptureMode { Click, Hook }

    [Header("Modo de captura")]
    public CaptureMode captureMode = CaptureMode.Click;
    [Header("Criterio de nivel (selecciona sólo uno)")]
    public Criterion currentCriterion;

    [Header("¿Cambiar objetivo tras cada acierto?")]
    public bool randomizeTargetOnCatch = false;

    [Header("Prefabs de peces")]
    public List<GameObject> fishPrefabs;

    [Header("Sprites de figura (Shape)")]
    public List<Sprite> shapeSprites;

    [Header("Spawn y movimiento")]
    public float spawnInterval = 0.5f;
    public float speed = 2f;
    public float amplitude = 1f;
    public float frequency = 2f;

    [Header("Meta de capturas")]
    public int targetCount = 10;

    // Objetivo actual
    public Species targetSpecies;
    public FishColor targetColor;
    public char targetLetter;
    public int targetNumber;
    public Sprite targetShape;

    private int caughtCount;
    public List<Fish> activeFish = new List<Fish>();

    // Listas dinámicas
    private List<Species> availableSpecies;
    private List<FishColor> availableColors;

    void Start()
    {
        // Construir listas únicas de especies y colores desde prefabs
        var speciesSet = new HashSet<Species>();
        var colorSet = new HashSet<FishColor>();
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
        availableColors = new List<FishColor>(colorSet);

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
    var go = Instantiate(prefab, GetSpawnPosition(), Quaternion.identity);
    var fish = go.GetComponent<Fish>();
    if (fish == null) return;

    // Reset identificación
    fish.letter = '\0';
    fish.number = null;
    if (currentCriterion == Criterion.Letter)
        fish.letter = (char)('A' + Random.Range(0, 26));
    else if (currentCriterion == Criterion.Number)
        fish.number = Random.Range(0, 10);
    if (currentCriterion == Criterion.Shape || currentCriterion == Criterion.ShapeAndColor)
        fish.shapeRenderer.sprite = shapeSprites[Random.Range(0, shapeSprites.Count)];

    // Suscribir colisiones
    fish.OnExitedScreen += OnFishExited;
    if (captureMode == CaptureMode.Click)
        fish.OnClicked += OnFishClicked;
    // En Hook mode, no suscribimos OnClicked: el Hook atrapará con OnTriggerEnter2D

    activeFish.Add(fish);
}
public void HandleHookCatch(Fish f)
{
    // Similar a OnFishClicked, pero sin OnClicked invocado
    if (!activeFish.Contains(f)) return;
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
            correct = f.species == targetSpecies && f.colorName == targetColor;
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
            correct = f.shapeRenderer.sprite == targetShape && f.colorName == targetColor;
            break;
    }

    if (correct)
    {
        caughtCount++;
        UIController.Instance.UpdateProgress(caughtCount, targetCount);
        if (randomizeTargetOnCatch)
            GenerateTargetValues();
        if (caughtCount >= targetCount)
            PickNewTarget();
    }
    else
    {
        // Puedes reproducir un sonido de error: HookController puede hacerlo.
    }

    activeFish.Remove(f);
    Destroy(f.gameObject);
}


    void Update()
    {
        for (int i = activeFish.Count - 1; i >= 0; i--)
        {
            var f = activeFish[i];
            float x = f.transform.position.x + speed * Time.deltaTime;
            float y = f.transform.position.y + Mathf.Sin(Time.time * frequency) * amplitude * Time.deltaTime;
            f.transform.position = new Vector2(x, y);
            f.CheckOffScreen();
        }
    }

    void OnFishExited(Fish f)
    {
        activeFish.Remove(f);
        Destroy(f.gameObject);
    }
    public void RemoveActiveFish(Fish f)
{
    if (activeFish.Contains(f))
        activeFish.Remove(f);
}

    public void OnFishClicked(Fish f)
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
                correct = f.species == targetSpecies && f.colorName == targetColor;
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
                correct = f.shapeRenderer.sprite == targetShape && f.colorName == targetColor;
                break;
        }

        if (correct)
        {
            caughtCount++;
            UIController.Instance.UpdateProgress(caughtCount, targetCount);

            if (randomizeTargetOnCatch)
            {
                // Genera un nuevo objetivo válido del mismo criterio, sin reiniciar el progreso
                GenerateTargetValues();
            }

            if (caughtCount >= targetCount)
            {
                PickNewTarget();
            }
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
                           Random.Range(cam.transform.position.y - h, cam.transform.position.y + h),
                           0f);
    }

   void PickNewTarget()
{
    caughtCount = 0;
    GenerateTargetValues();
    UIController.Instance.UpdateProgress(0, targetCount);
}

void GenerateTargetValues()
{
    switch (currentCriterion)
    {
        case Criterion.Color:
    targetColor = availableColors[Random.Range(0, availableColors.Count)];
    UIController.Instance.ShowColorTarget(
        GetColorFromEnum(targetColor),
        targetColor.ToString()
    );
    break;


        case Criterion.Species:
            targetSpecies = availableSpecies[Random.Range(0, availableSpecies.Count)];
            // Buscar sprite del prefab
            Sprite sp = null;
            var pf = fishPrefabs.Find(p => p.GetComponent<Fish>().species == targetSpecies);
            if (pf != null) sp = pf.GetComponent<SpriteRenderer>().sprite;
            // Muestra sprite, con texto opcional: si NO quieres texto de nombre, pasa label=null
            UIController.Instance.ShowSpriteTarget(sp, null, false);
            break;

        case Criterion.ColorAndSpecies:
            // Elegir combo válido
            var combos = new List<(Species, FishColor, Sprite)>();
            foreach (var p in fishPrefabs)
            {
                var f = p.GetComponent<Fish>();
                if (f != null)
                    combos.Add((f.species, f.colorName, p.GetComponent<SpriteRenderer>().sprite));
            }
            if (combos.Count > 0)
            {
                var choice = combos[Random.Range(0, combos.Count)];
                targetSpecies = choice.Item1;
                targetColor   = choice.Item2;
                Sprite sprite = choice.Item3;
                // Muestra sprite sin tint, opcionalmente texto
                UIController.Instance.ShowSpriteTarget(sprite, $"{targetColor} {targetSpecies}", false);
            }
            break;

        case Criterion.Letter:
            targetLetter = (char)('A' + Random.Range(0, 26));
            // Mostrar solo texto, con estilo letter/number:
            UIController.Instance.ShowTextTarget(targetLetter.ToString());
            break;

        case Criterion.Number:
            targetNumber = Random.Range(0, 10);
            UIController.Instance.ShowTextTarget(targetNumber.ToString());
            break;

        case Criterion.Shape:
            if (shapeSprites != null && shapeSprites.Count > 0)
            {
                targetShape = shapeSprites[Random.Range(0, shapeSprites.Count)];
                // Muestra sprite y texto con nombre del sprite (si quieres):
                UIController.Instance.ShowSpriteTarget(targetShape, targetShape.name, false);
            }
            break;

        case Criterion.ShapeAndColor:
            if (shapeSprites != null && shapeSprites.Count > 0 && availableColors.Count > 0)
            {
                targetShape = shapeSprites[Random.Range(0, shapeSprites.Count)];
                targetColor = availableColors[Random.Range(0, availableColors.Count)];
                // Muestra sprite y texto con color+nombre
                UIController.Instance.ShowSpriteTarget(targetShape, $"{targetColor} {targetShape.name}", false);
            }
            break;
    }
    // Reiniciar progreso:
    UIController.Instance.UpdateProgress(0, targetCount);
}


    Color GetColorFromEnum(FishColor c)
{
    switch (c)
    {
        case FishColor.Rojo:   return Color.red;
        case FishColor.Verde:  return Color.green;
        case FishColor.Azul:   return Color.blue;
        case FishColor.Morado: return new Color(0.6f, 0.2f, 0.8f);
        case FishColor.Blanco: return Color.white;
        default:               return Color.white;
    }
}


}
