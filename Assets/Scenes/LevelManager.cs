using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    // ——— 1. Singleton ————————————————————————
    public static LevelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }

        // Carga el ScriptableObject según el criterio
        currentConfig = criterionConfigs.Find(c => c.criterio == criterioSeleccionado);
        if (currentConfig == null)
            Debug.LogError($"Falta CriterionConfig para {criterioSeleccionado}");
    }
    // ————————————————————————————————————————

    public enum Criterio {
        Color,
        Especie,
        ColorYEspecie,
        Letra,
        Numero,
        Figura,
        FiguraYColor
    }

    [Header("Selección de Nivel")]
    public Criterio criterioSeleccionado;

    [Header("Configs disponibles")]
    [Tooltip("Un asset CriterionConfig por cada valor de Criterio")]
    public List<CriterionConfig> criterionConfigs;

    [HideInInspector]
    public CriterionConfig currentConfig;

    // ——— 2. Aquí añadimos el targetAttributes —————————————————
    [HideInInspector]
    public FishAttributes targetAttributes;
    // ——————————————————————————————————————————————

    [Header("Movimiento de peces")]
    public float amplitudSeno = 1f;
    public float frecuenciaSeno = 2f;
    public float velocidadHorizontal = 2f;

    [Header("Spawn de peces")]
    [Range(1,20)] public float pesoObjetivo = 5f;
    public float pesoBase = 1f;
    public int maximoPecesEnPantalla = 10;
    public float intervaloSpawn = 0.5f;
}
