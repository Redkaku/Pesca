using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShapeConfig",
    menuName = "Game/Configs/Shapes"
)]
public class ShapeConfig : ScriptableObject
{
    [Tooltip("Sprites de figuras que pueden llevar los peces")]
    public List<Sprite> shapeSprites = new List<Sprite>();
}
