using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CriterionConfig", menuName = "Game/Fish Criterion Config")]
public class CriterionConfig : ScriptableObject
{
    public LevelManager.Criterio criterio;       // El enum que cubre este config

    // Solo se usarán las listas relevantes según el criterio
    public List<Color> colores;
    public List<GameObject> especiesPrefab;
    public List<char> letras;
    public List<int> numeros;
    public List<Sprite> figuras;
}
