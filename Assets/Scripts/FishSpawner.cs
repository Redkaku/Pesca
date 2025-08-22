// FishSpawner.cs
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
    public int             id;        // << nuevo
    public CriterionType   type;
    public char            letter;
    public int?            number;
    public Species         species;
    public FishColor       colorEnum;
    public Color           tint;
    public Sprite          sprite;
    public string          label;
}

[RequireComponent(typeof(BoxCollider2D))]
public class FishSpawner : MonoBehaviour
{
    public enum CaptureMode { Click, Hook }

    [Header("Sonidos")]    public AudioClip applauseClip;
    AudioSource             audioSource;
    [Range(0f,0.45f)]
    [Tooltip("Porcentaje de margen desde arriba/abajo sin spawn")]
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
    public GameObject    waterBackground;
    public Transform     surfaceMarker;
    private float surfaceY;
    public float SurfaceY => surfaceY;
    public BoxCollider2D spawnZoneCollider;
    [Header("Probabilidad de objetivo")]
    [Range(0f,1f)] public float targetWeight = 0.3f;

    // Estructura Color + FishColor
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

    // Estado interno
    SimpleCriterionData activePreset;
    GameObject[] spawnPrefabs;
    Bounds       waterBounds;
    int          caughtCount;
    bool         spawning = true;

    int     letterIndex = 0, numberIndex = -1;
    int     nextTargetId = 1;        // << contador de objetivos
    public TargetInfo currentTarget;
    List<GameObject> targetPrefabs;
    float[] slotYs;
    bool    nextFromLeft = true;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        activePreset = presets[GameSettings.CriterionIndex];
        spawnPrefabs = activePreset.whitePrefabs;
        captureMode  = GameSettings.CaptureMode;
        targetWeight = GameSettings.FishSpawnProbability;

        // Ajusta targetCount según criterio
        switch(activePreset.criterionName){
            case "Letras":       targetCount=26; break;
            case "Numeros 1-10": targetCount=10; break;
            case "Numeros 1-20": targetCount=20; break;
            case "Vocales":      targetCount=10; break;
            default:             targetCount=12; break;
        }

        // Calcula bounds
        if (captureMode==CaptureMode.Click && waterBackground){
            var sr = waterBackground.GetComponent<SpriteRenderer>();
            waterBounds = sr!=null ? sr.bounds
                                   : waterBackground.GetComponent<Collider2D>().bounds;
        } else {
            waterBounds = spawnZoneCollider.bounds;
        }

        PrecalculateSlotYs();
        caughtCount = 0;
        UIController.Instance.UpdateProgress(0,targetCount);

        currentTarget = GenerateTargetInfo();
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop(){
        while(spawning){
            SpawnOneAlternate();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    // Compara colores con una pequeña tolerancia para evitar fallos por floats
bool ColorsEqual(Color a, Color b, float eps = 0.02f)
{
    return Mathf.Abs(a.r - b.r) <= eps
        && Mathf.Abs(a.g - b.g) <= eps
        && Mathf.Abs(a.b - b.b) <= eps
        && Mathf.Abs(a.a - b.a) <= eps;
}




    void PrecalculateSlotYs()
    {
        Bounds b = (captureMode==CaptureMode.Hook)
                 ? spawnZoneCollider.bounds
                 : waterBounds;

        float totalH = b.size.y;
        float m      = totalH * verticalMarginPercent;
        float yMin   = b.min.y + m;
        float yMax   = b.max.y - m;

        slotYs = new float[8];
        float h = (yMax - yMin) / 8f;
        for(int i=0;i<8;i++)
            slotYs[i] = yMin + h*(0.5f + i);
    }

    TargetInfo GenerateTargetInfo()
    {
        var named = GameSettings.UseAllColors
                    ? GetRandomNamedColor()
                    : new NamedColor(Color.white,FishColor.Blanco);

        var info = new TargetInfo {
            id        = nextTargetId++,     // << asigna un ID único
            colorEnum = named.name,
            tint      = named.color
        };

        switch(activePreset.criterionName){
          case "Letras":
            info.type   = CriterionType.Letter;
            info.letter = (char)('A' + (letterIndex++ % 26));
            info.label  = info.letter.ToString();
            break;

          case "Vocales":
            info.type   = CriterionType.Vowel;
            var v = activePreset.customLetters;
            info.letter = v[letterIndex++ % v.Length];
            info.label  = info.letter.ToString();
            break;

          case "Numeros 1-10":
          case "Numeros 1-20":
          case "CustomRange":
            info.type   = CriterionType.Number;
            int minN = activePreset.criterionName=="Numeros 1-20" ? 1 : activePreset.numberMin;
            int maxN = activePreset.criterionName=="Numeros 1-20" ? 20 : activePreset.numberMax;
            numberIndex = (numberIndex+1)%(maxN-minN+1);
            info.number = minN + numberIndex;
            info.label  = info.number.Value.ToString();
            break;

          default:
            info.type = CriterionType.Species;
            // Elige especie al azar...
            var randomPF = spawnPrefabs[Random.Range(0,spawnPrefabs.Length)];
            var fdata    = randomPF.GetComponent<Fish>();
            info.species = fdata.species;

            // Filtra prefabs por especie+color
            targetPrefabs = new List<GameObject>();
            foreach(var pf in activePreset.whitePrefabs){
                var f = pf.GetComponent<Fish>();
                if(f.species==info.species
                  && (!GameSettings.UseAllColors
                      || f.colorName==info.colorEnum))
                {
                    targetPrefabs.Add(pf);
                }
            }
            if(targetPrefabs.Count==0){
                foreach(var pf in activePreset.whitePrefabs)
                    if(pf.GetComponent<Fish>().species==info.species)
                        targetPrefabs.Add(pf);
            }
            if(targetPrefabs.Count==0)
                targetPrefabs.AddRange(spawnPrefabs);

            var chosen = targetPrefabs[Random.Range(0,targetPrefabs.Count)];
            info.sprite = chosen.GetComponentInChildren<SpriteRenderer>().sprite;
            info.label  = $"{info.species} {info.colorEnum}";
            break;
        }

        // Muestra en UI
        if(info.type==CriterionType.Species)
            UIController.Instance.ShowSpriteTarget(info.sprite, info.label, info.tint);
        else
            UIController.Instance.ShowTextTarget(info.label, info.tint);

        return info;
    }

    void SpawnOneAlternate()
    {
        
        bool spawnTarget = Random.value < targetWeight;

        // 1) lado + slot Y
        bool fromLeft = nextFromLeft; nextFromLeft = !nextFromLeft;
        var b = waterBounds;
        float x = fromLeft ? b.min.x : b.max.x;
        float y = slotYs[Random.Range(0,slotYs.Length)];
        Vector3 pos = new Vector3(x,y,0f);
        

        // 2) elige prefab
        GameObject prefab;
        switch(currentTarget.type){
          case CriterionType.Letter:
          case CriterionType.Vowel:
          case CriterionType.Number:
            prefab = spawnPrefabs[Random.Range(0,spawnPrefabs.Length)];
            break;
          default: // Species
            prefab = (spawnTarget && targetPrefabs.Count>0)
                     ? targetPrefabs[Random.Range(0,targetPrefabs.Count)]
                     : spawnPrefabs[Random.Range(0,spawnPrefabs.Length)];
            break;
        }

        // 3) instancia
        var go = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(go, 15f);

        // 4) flip
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if(sr!=null) sr.flipX = !fromLeft;

        // 5) color
        Color tint = spawnTarget
            ? currentTarget.tint
            : (GameSettings.UseAllColors
                ? GetRandomNamedColor().color
                : Color.white);

        // 6) asigna targetId y datos
        var fish = go.GetComponent<Fish>();
        fish.letter = '\0';
fish.number = null;
        fish.targetId = currentTarget.id;

        switch(currentTarget.type){
          case CriterionType.Letter:
            fish.letter = spawnTarget
                         ? currentTarget.letter
                         : (char)('A'+Random.Range(0,26));
            fish.letterText.color = tint;
            break;

          case CriterionType.Vowel:
            if (spawnTarget)
            fish.letter = currentTarget.letter;
        else
        {
            if (currentTarget.type == CriterionType.Vowel)
            {
                var arr = activePreset.customLetters;
                fish.letter = arr[Random.Range(0, arr.Length)];
            }
            else
            {
                fish.letter = (char)('A' + Random.Range(0, 26));
            }
        }
        // texto y color del texto
        if (fish.letterText != null)
            fish.letterText.color = tint;
            break;

          case CriterionType.Number:
            fish.number = spawnTarget
                         ? currentTarget.number
                         : Random.Range(
                             activePreset.numberMin,
                             activePreset.numberMax+1
                           );
            fish.numberText.color = tint;
            break;

          default: // Species
            // default: // Species
if (sr != null) sr.color = tint;

// Asigna color si corresponde
if (spawnTarget)
{
    fish.colorName = currentTarget.colorEnum;
}
else if (GameSettings.UseAllColors)
{
    var randomNamed = GetRandomNamedColor();
    fish.colorName = randomNamed.name;
    if (sr != null) sr.color = randomNamed.color;
}
else
{
    fish.colorName = FishColor.Blanco;
}

            break;
        }

        // 7) movimiento
        var mover = go.AddComponent<FishMovement2D>();
        float dir = fromLeft ? +1f : -1f;
        mover.speed     = baseSpeed * dir * (1f + GameSettings.LevelIndex*0.5f);
        mover.amplitude = amplitude;
        mover.frequency = frequency;

        // 8) eventos
        fish.OnExitedScreen += OnFishExited;
        if(captureMode==CaptureMode.Click)
            fish.OnClicked += HandleCapture;
    }

    public void HandleCapture(Fish fish)
    {
        if(fish==null) return;

        if (EvaluateCatch(fish))
        {
            caughtCount++;
            UIController.Instance.UpdateProgress(caughtCount, targetCount);
            currentTarget = GenerateTargetInfo();
            if(caughtCount>= targetCount) Victory();
        }
        Destroy(fish.gameObject);
    }

public bool EvaluateCatch(Fish fish)
{
    if (fish == null) return false;

    switch (currentTarget.type)
    {
        case CriterionType.Letter:
        case CriterionType.Vowel:
            // letra debe coincidir
            if (fish.letter != currentTarget.letter) return false;
            // si usamos colores, comparamos tolerante sobre el color del texto
            if (GameSettings.UseAllColors)
            {
                if (fish.letterText == null) return false;
                if (!ColorsEqual(fish.letterText.color, currentTarget.tint)) return false;
            }
            return true;

        case CriterionType.Number:
            if (!fish.number.HasValue) return false;
            if (fish.number.Value != currentTarget.number) return false;
            if (GameSettings.UseAllColors)
            {
                if (fish.numberText == null) return false;
                if (!ColorsEqual(fish.numberText.color, currentTarget.tint)) return false;
            }
            return true;

        case CriterionType.Species:
        default:
            // Para especies validamos enum (y enum color si aplica)
            bool ok = fish.species == currentTarget.species;
            if (GameSettings.UseAllColors)
                ok &= fish.colorName == currentTarget.colorEnum; // comparar enums es seguro
            return ok;
    }
}


    void OnFishExited(Fish fish)
    {
        if(fish!=null) Destroy(fish.gameObject);
    }

    void Victory()
    {
        spawning = false;
        foreach(var f in FindObjectsOfType<Fish>())
            Destroy(f.gameObject);

        mainPanel.SetActive(false);
        act.SetActive(true);

        if(GameSettings.SpecialEffectsEnabled && applauseClip!=null)
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
}

// FishMovement2D.cs (sin cambios)
public class FishMovement2D : MonoBehaviour
{
    [HideInInspector] public float speed, amplitude, frequency;
    float _t;
    void Update()
    {
        var p = transform.position;
        p.x += speed * Time.deltaTime;
        p.y += Mathf.Sin(_t * frequency) * amplitude * Time.deltaTime;
        transform.position = p;
        _t += Time.deltaTime;
    }
}
