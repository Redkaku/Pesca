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

    // Para Touch Mode: asigna aquí el objeto de fondo marino con SpriteRenderer o Collider2D
    [Header("Touch Mode: Fondo Marino")]
    public GameObject waterBackground;

    // Para Hook Mode: define superficie y profundidad de spawn
    [Header("Hook Mode: superficie y profundidad")]
    public Transform surfaceMarker; // define la Y de la superficie
    public float depthLimit = 5f;   // cuánto abajo de surfaceY pueden nacer peces

    // Objetivo actual
    [HideInInspector] public Species targetSpecies;
    [HideInInspector] public FishColor targetColor;
    [HideInInspector] public char targetLetter;
    [HideInInspector] public int targetNumber;
    [HideInInspector] public Sprite targetShape;

    private int caughtCount;
    public List<Fish> activeFish = new List<Fish>();

    // Listas dinámicas
    private List<Species> availableSpecies;
    private List<FishColor> availableColors;

    // Bounds del waterBackground para Touch Mode
    private Bounds waterBounds;
    // surfaceY para Hook Mode
    private float surfaceY;

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

        // Configurar waterBounds si Touch Mode y waterBackground asignado
        // Dentro de FishSpawner.Start():
        if (waterBackground != null)
        {
            var sr = waterBackground.GetComponent<SpriteRenderer>();
            if (sr != null) waterBounds = sr.bounds;
            else
            {
                var col = waterBackground.GetComponent<Collider2D>();
                if (col != null) waterBounds = col.bounds;
                else Debug.LogWarning("WaterBackground necesita SpriteRenderer o Collider2D");
            }
        }
if (waterBackground != null) {
    Debug.Log($"[FishSpawner] waterBounds minY={waterBounds.min.y}, maxY={waterBounds.max.y}");
}


        // Configurar surfaceY si Hook Mode
        if (surfaceMarker != null)
        {
            surfaceY = surfaceMarker.position.y;
        }
        else
        {
            // Si no tienes surfaceMarker, puedes tomar la Y de la cámara:
            surfaceY = Camera.main.transform.position.y;
        }

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
    void OnDrawGizmosSelected() {
    if (waterBackground != null) {
        var sr = waterBackground.GetComponent<SpriteRenderer>();
        Bounds b;
        if (sr != null) b = sr.bounds;
        else {
            var col = waterBackground.GetComponent<Collider2D>();
            if (col != null) b = col.bounds;
            else return;
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}


    void SpawnFish()
    {
        if (fishPrefabs.Count == 0) return;
        var prefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        Vector3 spawnPos = GetSpawnPosition();
        var go = Instantiate(prefab, spawnPos, Quaternion.identity);
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
            if (shapeSprites != null && shapeSprites.Count > 0)
                fish.shapeRenderer.sprite = shapeSprites[Random.Range(0, shapeSprites.Count)];

        // Suscribir colisiones
        fish.OnExitedScreen += OnFishExited;
        if (captureMode == CaptureMode.Click)
            fish.OnClicked += OnFishClicked;
        // En Hook mode, no suscribimos OnClicked: el Hook atrapará con OnTriggerEnter2D

        activeFish.Add(fish);
    }

    // Obtener posición de spawn según modo:
    Vector3 GetSpawnPosition() {
    if (captureMode == CaptureMode.Click && waterBackground != null) {
        // Touch Mode dentro de waterBounds:
       float x = Random.Range(waterBounds.min.x - 1f, waterBounds.min.x); // fuera a la izquierda
        float y = Random.Range(waterBounds.min.y, waterBounds.max.y);
        return new Vector3(x, y, 0f);
    }
    else if (captureMode == CaptureMode.Hook && waterBackground != null) {
        // Hook Mode pero spawnear aleatorio dentro de waterBounds:
        float x = Random.Range(waterBounds.min.x - 1f, waterBounds.min.x); // fuera a la izquierda
        float y = Random.Range(waterBounds.min.y, waterBounds.max.y);
        return new Vector3(x, y, 0f);
    }
    else {
        // Hook Mode sin waterBackground: usar depthLimit
        Camera cam = Camera.main;
        float camH = cam.orthographicSize;
        float camW = camH * cam.aspect;
        float x = cam.transform.position.x - camW - 1f;
        float minY = surfaceY - depthLimit;
        float maxY = surfaceY - 0.5f;
        float y = Random.Range(minY, maxY);
        return new Vector3(x, y, 0f);
    }
}



    public void HandleHookCatch(Fish f)
    {
        // NO comprobamos Contains temprano, procedemos siempre
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
                correct = (f.number.HasValue && f.number.Value == targetNumber);
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
            // El HookController reproduce sonido de error.
        }

        // Remover de la lista y destruir:
        if (activeFish.Contains(f))
            activeFish.Remove(f);
        Destroy(f.gameObject);
    }

    void Update()
    {
        // Mover cada pez
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
        if (activeFish.Contains(f))
            activeFish.Remove(f);
        Destroy(f.gameObject);
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
                correct = (f.number.HasValue && f.number.Value == targetNumber);
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
            // Aquí podrías reproducir sonido de error en Click Mode:
            // asegúrate de tener un AudioSource/clip en este script.
        }

        if (activeFish.Contains(f))
            activeFish.Remove(f);
        Destroy(f.gameObject);
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
                if (availableColors.Count > 0)
                {
                    targetColor = availableColors[Random.Range(0, availableColors.Count)];
                    UIController.Instance.ShowColorTarget(
                        GetColorFromEnum(targetColor),
                        targetColor.ToString()
                    );
                }
                break;
            case Criterion.Species:
                if (availableSpecies.Count > 0)
                {
                    targetSpecies = availableSpecies[Random.Range(0, availableSpecies.Count)];
                    Sprite sp = null;
                    var pf = fishPrefabs.Find(p => p.GetComponent<Fish>().species == targetSpecies);
                    if (pf != null) sp = pf.GetComponent<SpriteRenderer>().sprite;
                    UIController.Instance.ShowSpriteTarget(sp, null, false);
                }
                break;
            case Criterion.ColorAndSpecies:
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
                    targetColor = choice.Item2;
                    Sprite sprite = choice.Item3;
                    UIController.Instance.ShowSpriteTarget(sprite, $"{targetColor} {targetSpecies}", false);
                }
                break;
            case Criterion.Letter:
                targetLetter = (char)('A' + Random.Range(0, 26));
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
                    UIController.Instance.ShowSpriteTarget(targetShape, targetShape.name, false);
                }
                break;
            case Criterion.ShapeAndColor:
                if (shapeSprites != null && shapeSprites.Count > 0 && availableColors.Count > 0)
                {
                    targetShape = shapeSprites[Random.Range(0, shapeSprites.Count)];
                    targetColor = availableColors[Random.Range(0, availableColors.Count)];
                    UIController.Instance.ShowSpriteTarget(targetShape, $"{targetColor} {targetShape.name}", false);
                }
                break;
        }
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
