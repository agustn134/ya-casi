using UnityEngine;

/// <summary>Sprites originales de LimeZu para el Grid editable del edificio F.</summary>
[CreateAssetMenu(menuName = "Ya Casi/Paleta del edificio F")]
public sealed class LevelOneBuildingPalette : ScriptableObject
{
    [Header("Piso marfil y franjas gris azul")]
    public Sprite floor, lane;

    [Header("Pared crema con zoclo verde: dos filas de una celda")]
    public Sprite wallFace, wallFaceLower, wallTop, wallLeft, wallRight;
    public Sprite cornerTopLeft, cornerTopLeftLower, cornerTopRight, cornerTopRightLower;
    public Sprite cornerBottomLeft, cornerBottomLeftLower, cornerBottomRight, cornerBottomRightLower;

    [Header("Acentos y exterior")]
    public Sprite column, window, exteriorPaving, roof, bench, shrub;
}
