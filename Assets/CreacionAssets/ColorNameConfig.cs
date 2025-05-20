using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ColorNameConfig",
    menuName = "Game/Configs/Color Names"
)]
public class ColorNameConfig : ScriptableObject
{
    [Tooltip("Nombres de color que usarán los peces")]
    public List<string> colorNames = new List<string>();
}
