using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class FishSpawner : MonoBehaviour
{
    public enum CaptureMode { Click, Hook }
    [Header("Sonidos")]
    
    public AudioClip applauseClip;              // <— nuevo
    public  AudioSource audioSource;
    [Header("Ralentización & Escena siguiente")]
    [Tooltip("Objeto que aparece al ganar")]
    public GameObject act;
    [Range(0.01f,1f)]
    public float slowFactor = 0.2f;
    [Tooltip("Segundos reales antes de cargar la escena de burbujas")]
    public float victoryDelay = 2f;

    [Header("UI Panel A")]
    public GameObject mainPanel;

    [Header("Spawn y movimiento")]
    public float spawnInterval = 0.5f;
    public float baseSpeed     = 2f;
    public float amplitude     = 0.5f;
    public float frequency     = 2f;

    [Header("Meta de capturas")]
    public int targetCount = 10;

    [Header("Presets de criterio")]
    public SimpleCriterionData[] presets;

    [Header("Modo Touch / Hook")]
    public CaptureMode captureMode = CaptureMode.Click;

    [Header("Zonas")]
    public GameObject     waterBackground;      // para Click
    public Transform      surfaceMarker;        // para Hook
    public BoxCollider2D  spawnZoneCollider;   // zona de spawn (Hook)

    // --- Estado interno ---
    private SimpleCriterionData activePreset;
    private GameObject[]        spawnPrefabs;
    private Bounds              waterBounds;
    private float               surfaceY;
    private int                 caughtCount;
    private bool                spawning    = true;

    // secuencias de target
    private int    letterIndex = 0, numberIndex = 0;
    private char   targetLetter;
    private int    targetNumber;

    private string targetText;
    private Color  targetTextColor;
    private Species   targetSpecies;
    private FishColor targetColor;

    // para spawn en slots
    private float[] slotYs;
    private bool    spawnEvenGroup = true;  // alterna cada ciclo
    private bool nextFromLeft = true;    // alternador L/R
     [Range(0f,1f)]
    [Tooltip("Probabilidad de generar el objetivo en cada spawn")]
    public float targetWeight = 0.3f;
    private List<GameObject> targetPrefabs;
  

    void Start()
{
    audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
     // 1) UI inicial
        mainPanel.SetActive(true);
    act.SetActive(false);

    // 2) Cargar preset y prefabs
    activePreset = presets[GameSettings.CriterionIndex];
    spawnPrefabs = GameSettings.UseAllColors
        ? activePreset.coloredPrefabs
        : activePreset.whitePrefabs;
    captureMode = GameSettings.CaptureMode;

    // ————————————————————————————————————————————————————————————————
    //  2.1) Ajuste dinámico de la meta según el criterio:
    switch (activePreset.criterionName)
    {
        case "Letras":
            targetCount = 27;
            break;
        case "Numeros 1-10":
            targetCount = 10;
            break;
        case "Numeros 1-20":
            targetCount = 20;
            break;
        case "Vocales":
            targetCount = 10;
            break;
        default:
            targetCount = 12;
            break;
    }
    // ————————————————————————————————————————————————————————————————

    // 3) Calcular bounds
    if (captureMode == CaptureMode.Click && waterBackground != null)
    {
        var sr = waterBackground.GetComponent<SpriteRenderer>();
        waterBounds = sr != null
            ? sr.bounds
            : waterBackground.GetComponent<Collider2D>().bounds;
    }
    else
    {
        waterBounds = spawnZoneCollider.bounds;
    }
    if (captureMode == CaptureMode.Hook && surfaceMarker != null)
        surfaceY = surfaceMarker.position.y;

    // 4) Pre‑calcular las 8 alturas
    PrecalculateSlotYs();

    // 5) Setup UI & primer objetivo
    caughtCount = 0;
    // Aquí ya usamos la nueva targetCount
    UIController.Instance.UpdateProgress(0, targetCount);
    GenerateTargetValues();
    // 6) Construir targetPrefabs (solo para peces)
    if (activePreset.criterionName != "Letras"
     && activePreset.criterionName != "Vocales"
     && !activePreset.criterionName.StartsWith("Numeros"))
    {
        targetPrefabs = new List<GameObject>();
        foreach (var pf in spawnPrefabs)
        {
            var f = pf.GetComponent<Fish>();
            if (f != null
             && f.species == targetSpecies
             && (!GameSettings.UseAllColors || f.colorName == targetColor))
            {
                targetPrefabs.Add(pf);
            }
        }
    }

    // 7) Arrancar el loop de spawn
    spawning = true;
    StartCoroutine(SpawnLoop());
}


    IEnumerator SpawnLoop()
    {
        while (spawning)
        {
            SpawnOneAlternate();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
void PrecalculateSlotYs()
{
    var b = waterBounds;
    slotYs = new float[8];
    float h = b.size.y / 8f;
    for (int i = 0; i < 8; i++)
        slotYs[i] = b.min.y + h * (i + 0.5f);
}

   void SpawnOneAlternate()
{
    if (spawnPrefabs == null || spawnPrefabs.Length == 0) return;

    bool fromLeft = nextFromLeft;
    nextFromLeft = !nextFromLeft;

    // slot y posición X
    int slotIdx = Random.Range(0, slotYs.Length);
    float y     = slotYs[slotIdx];
    var b       = waterBounds;
    float x     = fromLeft ? b.min.x : b.max.x;

    // 1) ¿Spawn objetivo o aleatorio?
    bool spawnTarget = Random.value < GameSettings.FishSpawnProbability;
    GameObject prefab;

    // Para peces, usamos targetPrefabs; para letras/números/vocales, prefab siempre es el mismo:
    if (spawnTarget
        && targetPrefabs != null
        && targetPrefabs.Count > 0)
    {
        prefab = targetPrefabs[Random.Range(0, targetPrefabs.Count)];
    }
    else
    {
        prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
    }

    // 2) Instanciamos
    var go = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity);
    //Destruir pez para optimizar rendimiento
    Destroy(go, 15f);

    // 3) Giramos sprite si viene de la derecha
        var sr = go.GetComponentInChildren<SpriteRenderer>();
    if (sr != null) sr.flipX = !fromLeft;

    // 4) Asignamos valores:
    if (activePreset.criterionName == "Letras"
     || activePreset.criterionName == "Vocales"
     || activePreset.criterionName.StartsWith("Numeros"))
    {
        // Letras/Vocales/Números: diferenciar objetivo vs. aleatorio
        if (spawnTarget)
            ApplyTarget(go);
        else
        {
            // letra/número **aleatorio**
            ApplyCriterion(go);
            // además, si estamos en modo colores, asignamos color **aleatorio** de la paleta:
            if (GameSettings.UseAllColors
                && (activePreset.criterionName == "Letras"
                 || activePreset.criterionName == "Vocales"
                 || activePreset.criterionName.StartsWith("Numeros")))
            {
                var fish = go.GetComponent<Fish>();
                fish.letterText.color = GetRandomPaletteColor();
                if (fish.numberText != null)
                    fish.numberText.color = GetRandomPaletteColor();
            }
        }
    }
    else
    {
        // Peces: para spawnTarget ya escogimos de targetPrefabs, el resto de spawnPrefabs
        // no necesitan nada más aquí
    }

    // 5) Movimiento
    float hSpeed = (fromLeft ? +baseSpeed : -baseSpeed)
                   * (1f + GameSettings.LevelIndex * 0.5f);
    var mover = go.AddComponent<FishMovement2D>();
    mover.speed     = hSpeed;
    mover.amplitude = amplitude;
    mover.frequency = frequency;

    // 6) Eventos
    var fishComp = go.GetComponent<Fish>();
    if (fishComp != null)
{
    fishComp.OnExitedScreen += OnFishExited;

    if (captureMode == CaptureMode.Click)
        fishComp.OnClicked += HandleCapture;
}
}

    void ApplyTarget(GameObject go)
    {
        var fish = go.GetComponent<Fish>();
        if (fish == null) return;

        switch (activePreset.criterionName)
        {
            case "Letras":
            case "Vocales":
                fish.letter = targetLetter;
                fish.letterText.color = targetTextColor;
                break;
            case "Numeros 1-10":
            case "Numeros 1-20":
            case "CustomRange":
                fish.number = targetNumber;
                fish.numberText.color = targetTextColor;
                break;
            default:
                // En este caso el prefab ya vino filtrado con la especie/color correctos
                break;
        }
    }


    void ApplyCriterion(GameObject go)
    {
        var fish = go.GetComponent<Fish>();
        if (fish==null) return;
        switch(activePreset.criterionName)
        {
            case "Letras":
                fish.letter = (char)('A'+Random.Range(0,26));
                fish.letterText.color = targetTextColor;
                break;
            case "Vocales":
                var v = activePreset.customLetters;
                fish.letter = v[Random.Range(0,v.Length)];
                fish.letterText.color = targetTextColor;
                break;
            case "Numeros 1-10":
            case "Numeros 1-20":
            case "CustomRange":
                int min,max;
                if(activePreset.criterionName=="Numeros 1-10"){min=1;max=10;}
                else if(activePreset.criterionName=="Numeros 1-20"){min=1;max=20;}
                else {min=activePreset.numberMin; max=activePreset.numberMax;}
                fish.number = Random.Range(min,max+1);
                fish.numberText.color = targetTextColor;
                break;
            default:
                // especies/colors
                break;
        }
    }

    public void HandleCapture(Fish fish)
    {
        if (!fish) return;
        if (EvaluateCatch(fish))
        {
            caughtCount++;
            UIController.Instance.UpdateProgress(caughtCount, targetCount);
            GenerateTargetValues();
            if (caughtCount >= targetCount) Victory();
        }
        Destroy(fish.gameObject);
    }

    public bool EvaluateCatch(Fish fish)
    {
        switch(activePreset.criterionName)
        {
            case "Letras":
            case "Vocales":
                return fish.letter==targetLetter
                    && fish.letterText.color==targetTextColor;
            case "Numeros 1-10":
            case "Numeros 1-20":
            case "CustomRange":
                return fish.number.HasValue
                    && fish.number.Value==targetNumber
                    && fish.numberText.color==targetTextColor;
            default:
                bool ok = fish.species==targetSpecies;
                if (GameSettings.UseAllColors) ok &= fish.colorName==targetColor;
                return ok;
        }
    }

    void OnFishExited(Fish fish)
    {
        if (fish && fish.gameObject) Destroy(fish.gameObject);
    }

    void GenerateTargetValues()
    {
        switch (activePreset.criterionName)
        {
            case "Letras":
                if (letterIndex >= 26) letterIndex = 0;
                targetLetter    = (char)('A' + letterIndex++);
                targetText      = targetLetter.ToString();
                targetTextColor = GameSettings.UseAllColors
                                  ? GetRandomPaletteColor()
                                  : Color.white;
                UIController.Instance.ShowTextTarget(targetText);
                UIController.Instance.ShowTextTarget(
    targetText,
    targetTextColor
);
                break;

            case "Vocales":
                var cv = activePreset.customLetters;
                if (letterIndex >= cv.Length) letterIndex = 0;
                targetLetter    = cv[letterIndex++];
                targetText      = targetLetter.ToString();
                targetTextColor = GameSettings.UseAllColors
                                  ? GetRandomPaletteColor()
                                  : Color.white;
                UIController.Instance.ShowTextTarget(targetText);
                UIController.Instance.ShowTextTarget(
    targetText,
    targetTextColor
);
                break;

            case "Numeros 1-10":
            case "Numeros 1-20":
            case "CustomRange":
                int minN, maxN;
                if (activePreset.criterionName=="Numeros 1-10"){minN=1;maxN=10;}
                else if(activePreset.criterionName=="Numeros 1-20"){minN=1;maxN=20;}
                else {minN=activePreset.numberMin; maxN=activePreset.numberMax;}
                int len = maxN-minN+1;
                if (numberIndex>=len) numberIndex=0;
                targetNumber    = minN + numberIndex++;
                targetText      = targetNumber.ToString();
                targetTextColor = GameSettings.UseAllColors
                                  ? GetRandomPaletteColor()
                                  : Color.white;
                UIController.Instance.ShowTextTarget(targetText);
               UIController.Instance.ShowTextTarget(
    targetText,
    targetTextColor
);
                break;

            default:
                var sample = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
                var fdata  = sample.GetComponent<Fish>();
                targetSpecies = fdata.species;
                targetColor   = fdata.colorName;
                var sr = sample.GetComponentInChildren<SpriteRenderer>();
Sprite sprite = sr != null ? sr.sprite : null;
UIController.Instance.ShowSpriteTarget(
    sprite,
    GameSettings.UseAllColors ? $"{targetColor} {targetSpecies}" : null
);

                break;
        }
    }

    void Victory()
    {
        spawning = false;
        foreach (var f in FindObjectsOfType<Fish>())
            Destroy(f.gameObject);

        mainPanel.SetActive(false);
        act.SetActive(true);

        // Solo reproducir “applause” si los efectos especiales están ON
        if (GameSettings.SpecialEffectsEnabled && applauseClip != null)
            audioSource.PlayOneShot(applauseClip);

        Time.timeScale = slowFactor;
        StartCoroutine(LoadBubbleSceneAfterDelay());
    }

    IEnumerator LoadBubbleSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(victoryDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MinijuegoBurbujas");
    }

    private static readonly Color[] _palette = {
        Color.white, Color.red, Color.blue, Color.yellow,
        new Color(1f,0.4f,0.7f), Color.green, Color.white,
        new Color(0.6f,0.2f,0.8f), new Color(0.65f,0.5f,0.4f),
        new Color(0.5f,0.5f,0.5f)
    };
    private Color GetRandomPaletteColor()
    {
        return _palette[Random.Range(0,_palette.Length)];
    }
}

// Este componente maneja el movimiento de cada pez:
public class FishMovement2D : MonoBehaviour
{
    [HideInInspector] public float speed;
    [HideInInspector] public float amplitude;
    [HideInInspector] public float frequency;
    private float _time;

    void Update()
    {
        Vector2 pos = transform.position;
        pos.x += speed * Time.deltaTime;
        pos.y += Mathf.Sin(_time * frequency) * amplitude * Time.deltaTime;
        transform.position = pos;
        _time += Time.deltaTime;
    }
}
