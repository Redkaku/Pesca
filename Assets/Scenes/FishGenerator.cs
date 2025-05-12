using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FishGenerator : MonoBehaviour
{
    public event Action<Fish, bool> OnFishCaught;

    // Cada opción es un conjunto de atributos + su peso para el spawn
    private struct FishOption
    {
        public FishAttributes attributes;
        public float weight;
    }

    [Header("Prefab de pez")]
    [Tooltip("Prefab genérico con el script Fish y sus componentes Visuales")]
    public GameObject fishPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Número máximo de peces en pantalla")]
    public int maximoPecesEnPantalla = 10;
    [Tooltip("Segundos entre cada intento de spawn")]
    public float intervaloSpawn = 0.5f;

    private LevelManager level;
    private List<FishOption> options;
    private int currentFishCount;

    // Límites calculados de la cámara
    private float leftBoundX, rightBoundX, bottomY, topY;

    private void Start()
    {
        level = LevelManager.Instance;

        // 1) Calculamos bordes de pantalla (un margen extra para spawn/despawn)
        var cam = Camera.main;
        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
        leftBoundX  = bl.x - 1f;
        rightBoundX = tr.x + 1f;
        bottomY     = bl.y;
        topY        = tr.y;

        // 2) Preparamos las opciones según el criterio y el config activo
        BuildOptions();
        ChooseAndShowTarget();

        // 3) Arrancamos el loop de spawn
        StartCoroutine(SpawnLoop());
    }
    private Vector3 GetSpawnPosition()
{
    Camera cam = Camera.main;
    float camHeight = 2f * cam.orthographicSize;
    float camWidth = camHeight * cam.aspect;

    float leftX = cam.transform.position.x - camWidth / 2f - 1f; // fuera de pantalla izquierda
    float randomY = UnityEngine.Random.Range(
        cam.transform.position.y - cam.orthographicSize + 1f,
        cam.transform.position.y + cam.orthographicSize - 1f
    );

    return new Vector3(leftX, randomY, 0f);
}

    // Construye la lista completa de atributos posibles con peso base
    private void BuildOptions()
    {
        options = new List<FishOption>();
        var cfg = level.currentConfig;

        switch (level.criterioSeleccionado)
        {
            case LevelManager.Criterio.Color:
                foreach (var col in cfg.colores)
                    options.Add(new FishOption {
                        attributes = new FishAttributes { color = col },
                        weight     = level.pesoBase
                    });
                break;

            case LevelManager.Criterio.Especie:
                foreach (var prefab in cfg.especiesPrefab)
                    options.Add(new FishOption {
                        attributes = new FishAttributes { speciesPrefab = prefab },
                        weight     = level.pesoBase
                    });
                break;

            case LevelManager.Criterio.ColorYEspecie:
                foreach (var col in cfg.colores)
                foreach (var prefab in cfg.especiesPrefab)
                    options.Add(new FishOption {
                        attributes = new FishAttributes {
                            color         = col,
                            speciesPrefab = prefab
                        },
                        weight = level.pesoBase
                    });
                break;

            case LevelManager.Criterio.Letra:
                foreach (var l in cfg.letras)
                    options.Add(new FishOption {
                        attributes = new FishAttributes { letter = l },
                        weight     = level.pesoBase
                    });
                break;

            case LevelManager.Criterio.Numero:
                foreach (var n in cfg.numeros)
                    options.Add(new FishOption {
                        attributes = new FishAttributes { number = n },
                        weight     = level.pesoBase
                    });
                break;

            case LevelManager.Criterio.Figura:
                foreach (var shape in cfg.figuras)
                    options.Add(new FishOption {
                        attributes = new FishAttributes {
                            shapeSprite = shape,
                            shapeColor  = Color.white
                        },
                        weight = level.pesoBase
                    });
                break;

            case LevelManager.Criterio.FiguraYColor:
                foreach (var shape in cfg.figuras)
                foreach (var col in cfg.colores)
                    options.Add(new FishOption {
                        attributes = new FishAttributes {
                            shapeSprite = shape,
                            shapeColor  = col
                        },
                        weight = level.pesoBase
                    });
                break;
        }
    }

    // Elige el valor objetivo, repondera y notifica a la UI
    private void ChooseAndShowTarget()
    {
        var cfg = level.currentConfig;

        switch (level.criterioSeleccionado)
        {
            case LevelManager.Criterio.Color:
            {
                var tc = cfg.colores[UnityEngine.Random.Range(0, cfg.colores.Count)];
                level.targetAttributes = new FishAttributes { color = tc };
                ReweightOptions(attr => attr.color == tc);
                UIController.Instance.ShowCriterionColor(tc);
                break;
            }

            case LevelManager.Criterio.Especie:
            {
                var tp = cfg.especiesPrefab[UnityEngine.Random.Range(0, cfg.especiesPrefab.Count)];
                level.targetAttributes = new FishAttributes { speciesPrefab = tp };
                ReweightOptions(attr => attr.speciesPrefab == tp);
                var spr = tp.GetComponent<SpriteRenderer>().sprite;
                UIController.Instance.ShowCriterionSpecies(spr);
                break;
            }

            case LevelManager.Criterio.ColorYEspecie:
            {
                var tc2 = cfg.colores[UnityEngine.Random.Range(0, cfg.colores.Count)];
                var tp2 = cfg.especiesPrefab[UnityEngine.Random.Range(0, cfg.especiesPrefab.Count)];
                level.targetAttributes = new FishAttributes {
                    color         = tc2,
                    speciesPrefab = tp2
                };
                ReweightOptions(attr => 
                    attr.color == tc2 && attr.speciesPrefab == tp2
                );
                UIController.Instance.ShowCriterionColorAndSpecies(
                    tc2,
                    tp2.GetComponent<SpriteRenderer>().sprite
                );
                break;
            }

            case LevelManager.Criterio.Letra:
            {
                var tl = cfg.letras[UnityEngine.Random.Range(0, cfg.letras.Count)];
                level.targetAttributes = new FishAttributes { letter = tl };
                ReweightOptions(attr => attr.letter == tl);
                UIController.Instance.ShowCriterionLetter(tl);
                break;
            }

            case LevelManager.Criterio.Numero:
            {
                var tn = cfg.numeros[UnityEngine.Random.Range(0, cfg.numeros.Count)];
                level.targetAttributes = new FishAttributes { number = tn };
                ReweightOptions(attr => attr.number == tn);
                UIController.Instance.ShowCriterionNumber(tn);
                break;
            }

            case LevelManager.Criterio.Figura:
            {
                var tf = cfg.figuras[UnityEngine.Random.Range(0, cfg.figuras.Count)];
                level.targetAttributes = new FishAttributes {
                    shapeSprite = tf,
                    shapeColor  = Color.white
                };
                ReweightOptions(attr => attr.shapeSprite == tf);
                UIController.Instance.ShowCriterionShape(tf);
                break;
            }

            case LevelManager.Criterio.FiguraYColor:
            {
                var tf2 = cfg.figuras[UnityEngine.Random.Range(0, cfg.figuras.Count)];
                var tc3 = cfg.colores[UnityEngine.Random.Range(0, cfg.colores.Count)];
                level.targetAttributes = new FishAttributes {
                    shapeSprite = tf2,
                    shapeColor  = tc3
                };
                ReweightOptions(attr => 
                    attr.shapeSprite == tf2 && attr.shapeColor == tc3
                );
                UIController.Instance.ShowCriterionShapeAndColor(tf2, tc3);
                break;
            }
        }
    }

    // Ajusta pesos para favorecer las opciones objetivo
    private void ReweightOptions(Func<FishAttributes, bool> isTarget)
    {
        for (int i = 0; i < options.Count; i++)
        {
            bool match = isTarget(options[i].attributes);
            options[i] = new FishOption
            {
                attributes = options[i].attributes,
                weight     = match ? level.pesoObjetivo : level.pesoBase
            };
        }
    }

    // Corutina que intenta spawnear cada intervalo
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (currentFishCount < maximoPecesEnPantalla)
                SpawnOneFish();
            yield return new WaitForSeconds(intervaloSpawn);
        }
    }

    // Instancia un pez en el borde izquierdo dentro de los límites de cámara
    private void SpawnOneFish()
    {
        // Muestreo ponderado
        float total = options.Sum(o => o.weight);
        float pick  = UnityEngine.Random.value * total;
        float acc   = 0f;
        var chosen  = options[0].attributes;
        foreach (var opt in options)
        {
            acc += opt.weight;
            if (pick <= acc)
            {
                chosen = opt.attributes;
                break;
            }
        }

        // Instancia
        var go = Instantiate(fishPrefab);
        float y = UnityEngine.Random.Range(bottomY, topY);
        go.transform.position = new Vector2(leftBoundX, y);

        // Configura movimiento y atributos
        var fish = go.GetComponent<Fish>();
        fish.SetAttributes(chosen);
        fish.InitMovement(
    level.velocidadHorizontal,
    level.amplitudSeno,
    level.frecuenciaSeno
);
Debug.Log($"[FishGenerator] Llamada InitMovement con speed={level.velocidadHorizontal}");



        currentFishCount++;
        fish.OnExitedScreen += HandleFishExited;
        fish.OnCaughtCorrect += HandleFishCaught;
    }

    private void HandleFishExited(Fish f)
    {
        currentFishCount--;
        f.OnExitedScreen -= HandleFishExited;
        Destroy(f.gameObject);
    }

    private void HandleFishCaught(Fish f, bool wasCorrect)
    {
        currentFishCount--;
        f.OnExitedScreen  -= HandleFishExited;
        f.OnCaughtCorrect -= HandleFishCaught;
        Destroy(f.gameObject);
        OnFishCaught?.Invoke(f, wasCorrect);
    }
}
