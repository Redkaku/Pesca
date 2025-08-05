using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// --- 1) Enum de colores ---
public enum FishColor
{
    Blanco, Rojo, Azul, Amarillo, Rosa,
    Verde, Cyan, Morado, Cafe, Gris
}

// --- 2) Clase para describir cualquier objetivo ---
public enum CriterionType { Letter, Vowel, Number, Species }
public class TargetInfo
{
  public CriterionType type;
  public char letter;
  public int? number;
  public Species species;
  public FishColor colorEnum;
  public Color tint;
  public Sprite sprite;
  public string label;
}


[RequireComponent(typeof(BoxCollider2D))]
public class FishSpawner : MonoBehaviour
{
    public enum CaptureMode { Click, Hook }

    [Header("Sonidos")] public AudioClip applauseClip;
    AudioSource audioSource;
    [Range(0f,0.45f)]
[Tooltip("Porcentaje de margen desde arriba/abajo que se reserva sin spawn (0 = sin margen, 0.45 = 45% de la altura)")]
public float verticalMarginPercent = 0.1f;

    [Header("Ralentización & Escena siguiente")]
    public GameObject act;
    [Range(0.01f,1f)] public float slowFactor = 0.2f;
    public float victoryDelay = 2f;

    [Header("UI Panel A")] public GameObject mainPanel;

    [Header("Spawn y movimiento")]
    public float spawnInterval = 0.5f;
    public float baseSpeed     = 2f;
    public float amplitude     = 0.5f;
    public float frequency     = 2f;

    [Header("Meta de capturas")] public int targetCount = 10;

    [Header("Presets de criterio")]
    public SimpleCriterionData[] presets;

    [Header("Modo Touch / Hook")]
    public CaptureMode captureMode = CaptureMode.Click;

    [Header("Zonas")]
    public GameObject     waterBackground;
    public Transform      surfaceMarker;
    public BoxCollider2D  spawnZoneCollider;

    [Header("Probabilidad de objetivo")]
    [Range(0f,1f)] public float targetWeight = 0.3f;

    // --- Estructura para emparejar Color+Criterio Enum ---
    struct NamedColor { public Color color; public FishColor name; 
        public NamedColor(Color c, FishColor n){ color=c; name=n; } }
    static readonly NamedColor[] _namedPalette = {
        new NamedColor(Color.white, FishColor.Blanco),
        new NamedColor(Color.red,   FishColor.Rojo),
        new NamedColor(Color.blue,  FishColor.Azul),
        new NamedColor(Color.yellow,FishColor.Amarillo),
        new NamedColor(new Color(1f,0.4f,0.7f),FishColor.Rosa),
        new NamedColor(Color.green, FishColor.Verde),
        new NamedColor(Color.cyan,  FishColor.Cyan),
        new NamedColor(new Color(0.6f,0.2f,0.8f),FishColor.Morado),
        new NamedColor(new Color(0.65f,0.5f,0.4f),FishColor.Cafe),
        new NamedColor(new Color(0.5f,0.5f,0.5f),FishColor.Gris),
    };
    NamedColor GetRandomNamedColor() => _namedPalette[Random.Range(0,_namedPalette.Length)];

    // --- Estado interno ---
    SimpleCriterionData activePreset;
    GameObject[] spawnPrefabs;
    Bounds       waterBounds;
    float        surfaceY;
    int          caughtCount;
    bool         spawning = true;

    int    letterIndex = 0, numberIndex = -1;
    TargetInfo currentTarget;
    List<GameObject> targetPrefabs;
    float[] slotYs;
    bool    nextFromLeft = true;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        targetWeight = GameSettings.FishSpawnProbability;

        activePreset = presets[GameSettings.CriterionIndex];
        spawnPrefabs = activePreset.whitePrefabs;
        captureMode  = GameSettings.CaptureMode;

        switch(activePreset.criterionName){
            case "Letras":       targetCount=26; break;
            case "Numeros 1-10": targetCount=10; break;
            case "Numeros 1-20": targetCount=20; break;
            case "Vocales":      targetCount= 10; break;
            default:             targetCount=12; break;
        }

        if(captureMode==CaptureMode.Click && waterBackground){
            var sr = waterBackground.GetComponent<SpriteRenderer>();
            waterBounds = sr!=null ? sr.bounds : waterBackground.GetComponent<Collider2D>().bounds;
        } else {
            waterBounds = spawnZoneCollider.bounds;
            if(surfaceMarker) surfaceY = surfaceMarker.position.y;
        }

        PrecalculateSlotYs();
        caughtCount=0;
        UIController.Instance.UpdateProgress(0,targetCount);

        currentTarget = GenerateTargetInfo();
        spawning=true;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop(){
        while(spawning){
            SpawnOneAlternate();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void PrecalculateSlotYs()
{
    // 1) Decide qué bounds usar:
    Bounds b = captureMode == CaptureMode.Hook
        ? spawnZoneCollider.bounds
        : waterBounds;

    // 2) Calcula márgenes en Y:
    float totalHeight = b.size.y;
    float margin      = totalHeight * verticalMarginPercent;
    float yMin        = b.min.y + margin;
    float yMax        = b.max.y - margin;

    // 3) Divide el espacio restante en 8 slots:
    slotYs = new float[8];
    float h = (yMax - yMin) / 8f;
    for (int i = 0; i < 8; i++)
        slotYs[i] = yMin + h * (0.5f + i);
}

    TargetInfo GenerateTargetInfo(){
        var named = GameSettings.UseAllColors
                    ? GetRandomNamedColor()
                    : new NamedColor(Color.white,FishColor.Blanco);

        var info = new TargetInfo{
            colorEnum=named.name,
            tint     =named.color
        };

        switch(activePreset.criterionName){
          case "Letras":
            info.type   = CriterionType.Letter;
            info.letter = (char)('A'+(letterIndex++%26));
            info.label  = info.letter.ToString();
            break;

          case "Vocales":
            info.type   = CriterionType.Vowel;
            var v = activePreset.customLetters;
            info.letter = v[letterIndex++%v.Length];
            info.label  = info.letter.ToString();
            break;

          case "Numeros 1-10":
          case "Numeros 1-20":
          case "CustomRange":
            info.type   = CriterionType.Number;
            int minN = activePreset.criterionName=="Numeros 1-20"?1:activePreset.numberMin;
            int maxN = activePreset.criterionName=="Numeros 1-20"?20:activePreset.numberMax;
            numberIndex=(numberIndex+1)%(maxN-minN+1);
            info.number = minN+numberIndex;
            info.label  = info.number.Value.ToString();
            break;

          default:
    info.type  = CriterionType.Species;
    // ya tienes info.colorEnum y info.tint
    // elige primero una especie **aleatoria** del pool blanco:
    {
        // 1) Elige un prefab de spawnPrefabs al azar sólo para obtener la especie
        var randomPF = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
        var fdata    = randomPF.GetComponent<Fish>();
        info.species = fdata.species;
    }

    // 2) Reconstruye targetPrefabs FILTRANDO POR ESPECIE+COLOR
    targetPrefabs = new List<GameObject>();
    foreach (var pf in activePreset.whitePrefabs)
    {
        var f = pf.GetComponent<Fish>();
        if (f.species == info.species
         && (!GameSettings.UseAllColors || f.colorName == info.colorEnum))
        {
            targetPrefabs.Add(pf);
        }
    }
    // si no hay ninguno, al menos todos de la misma especie (sin color)
    if (targetPrefabs.Count == 0)
    {
        foreach (var pf in activePreset.whitePrefabs)
            if (pf.GetComponent<Fish>().species == info.species)
                targetPrefabs.Add(pf);
    }
    // si aún no hay nada, fallback total
    if (targetPrefabs.Count == 0)
        targetPrefabs.AddRange(spawnPrefabs);

    // 3) Ahora SÍ eliges tu sprite del conjunto filtrado
    var chosenPF = targetPrefabs[Random.Range(0, targetPrefabs.Count)];
    info.sprite = chosenPF.GetComponentInChildren<SpriteRenderer>().sprite;
    info.label  = $"{info.species} {info.colorEnum}";
    break;

        }

        // muestro en UI
        if(info.type==CriterionType.Species)
            UIController.Instance.ShowSpriteTarget(info.sprite, info.label, info.tint);
        else
            UIController.Instance.ShowTextTarget(info.label, info.tint);

        return info;
    }


void SpawnOneAlternate()
{
    bool spawnTarget = Random.value < targetWeight;

    // 1) Decide lado y posición Y
    bool fromLeft = nextFromLeft;
    nextFromLeft = !nextFromLeft;
    int slotIdx = Random.Range(0, slotYs.Length);
    float y = slotYs[slotIdx];
    float x = fromLeft ? waterBounds.min.x : waterBounds.max.x;
    Vector3 spawnPos = new Vector3(x, y, 0f);

    // 2) Elige prefab
    GameObject prefab;
    switch (currentTarget.type)
    {
        case CriterionType.Letter:
        case CriterionType.Vowel:
        case CriterionType.Number:
            // Para letras/números/vocales, siempre del pool blanco
            prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
            break;

        case CriterionType.Species:
        default:
            // Para especies/figuras, aplica probabilidad sobre targetPrefabs
            if (spawnTarget && targetPrefabs.Count > 0)
                prefab = targetPrefabs[Random.Range(0, targetPrefabs.Count)];
            else
                prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
            break;
    }

    // 3) Instancia y gira sprite para mirar al centro
    var go = Instantiate(prefab, spawnPos, Quaternion.identity);
    var sr = go.GetComponentInChildren<SpriteRenderer>();
    if (sr != null) sr.flipX = !fromLeft;

    // 4) Calcula color: targetTextColor si spawnTarget, si no aleatorio o blanco
    Color tint = spawnTarget
        ? currentTarget.tint
        : (GameSettings.UseAllColors ? GetRandomNamedColor().color : Color.white);

    // 5) Asigna datos (letra/número o especie) y color
    var fish = go.GetComponent<Fish>();
    switch (currentTarget.type)
    {
        case CriterionType.Letter:
            if (spawnTarget)
            {
                fish.letter = currentTarget.letter;
            }
            else
            {
                fish.letter = (char)('A' + Random.Range(0, 26));
            }
            fish.letterText.color = tint;
            break;

        case CriterionType.Vowel:
            if (spawnTarget)
            {
                fish.letter = currentTarget.letter;
            }
            else
            {
                var v = activePreset.customLetters;
                fish.letter = v[Random.Range(0, v.Length)];
            }
            fish.letterText.color = tint;
            break;

        case CriterionType.Number:
            if (spawnTarget)
            {
                fish.number = currentTarget.number;
            }
            else
            {
                int minN = activePreset.criterionName == "Numeros 1-20" ? 1 : activePreset.numberMin;
                int maxN = activePreset.criterionName == "Numeros 1-20" ? 20 : activePreset.numberMax;
                fish.number = Random.Range(minN, maxN + 1);
            }
            fish.numberText.color = tint;
            break;

        case CriterionType.Species:
        default:
            // Para especies/figuras, el prefab ya es correcto si spawnTarget,
            // o aleatorio si no; simplemente aplicamos color al sprite:
            if (sr != null) sr.color = tint;
            // Guarda en el componente la especie/color para validar luego:
            fish.species   = spawnTarget ? currentTarget.species : fish.species;
            fish.colorName = spawnTarget ? currentTarget.colorEnum : fish.colorName;
            break;
    }

    // 6) Movimiento en X según fromLeft
    float dir = fromLeft ? +1f : -1f;
    var mover = go.AddComponent<FishMovement2D>();
    mover.speed     = baseSpeed * dir * (1f + GameSettings.LevelIndex * 0.5f);
    mover.amplitude = amplitude;
    mover.frequency = frequency;

    // 7) Eventos
    fish.OnExitedScreen += OnFishExited;
    if (captureMode == CaptureMode.Click)
        fish.OnClicked += HandleCapture;
}





    public void HandleCapture(Fish fish){
        if(fish==null) return;
        if(EvaluateCatch(fish)){
            caughtCount++;
            UIController.Instance.UpdateProgress(caughtCount,targetCount);
            currentTarget = GenerateTargetInfo();
            if(caughtCount>=targetCount) Victory();
        }
        Destroy(fish.gameObject);
    }

    public bool EvaluateCatch(Fish fish){
        switch(currentTarget.type){
          case CriterionType.Letter:
          case CriterionType.Vowel:
            if(fish.letter!=currentTarget.letter) return false;
            if(GameSettings.UseAllColors && fish.letterText.color!=currentTarget.tint)
              return false;
            return true;
          case CriterionType.Number:
            if(!fish.number.HasValue||fish.number.Value!=currentTarget.number) 
              return false;
            if(GameSettings.UseAllColors && fish.numberText.color!=currentTarget.tint)
              return false;
            return true;
          default:
            bool ok = fish.species==currentTarget.species;
            if(GameSettings.UseAllColors)
              ok &= fish.colorName==currentTarget.colorEnum;
            return ok;
        }
    }

    void OnFishExited(Fish fish){
        if(fish!=null) Destroy(fish.gameObject);
    }

    void Victory(){
        spawning=false;
        foreach(var f in FindObjectsOfType<Fish>()) Destroy(f.gameObject);
        mainPanel.SetActive(false);
        act.SetActive(true);
        if(GameSettings.SpecialEffectsEnabled && applauseClip!=null)
            audioSource.PlayOneShot(applauseClip);
        Time.timeScale=slowFactor;
        StartCoroutine(LoadBubbleSceneAfterDelay());
    }

    IEnumerator LoadBubbleSceneAfterDelay(){
        yield return new WaitForSecondsRealtime(victoryDelay);
        Time.timeScale=1f;
        SceneManager.LoadScene("MinijuegoBurbujas");
    }
}

// componente de movimiento sin cambios
public class FishMovement2D : MonoBehaviour {
    [HideInInspector] public float speed, amplitude, frequency;
    float _t;
    void Update(){
        var p=transform.position;
        p.x += speed*Time.deltaTime;
        p.y += Mathf.Sin(_t*frequency)*amplitude*Time.deltaTime;
        transform.position=p;
        _t+=Time.deltaTime;
    }
}
