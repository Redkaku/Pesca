using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpeciesEntry
{
    public string name;
}

[CreateAssetMenu(
    fileName = "SpeciesConfig",
    menuName = "Game/Configs/Species"
)]
public class SpeciesConfig : ScriptableObject
{
    [Tooltip("Lista de especies disponibles con su sprite")]
    public List<SpeciesEntry> species = new List<SpeciesEntry>();
}
