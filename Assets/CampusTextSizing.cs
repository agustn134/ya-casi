using TMPro;
using UnityEngine;

/// <summary>Reduce los paneles del campus sin encogerlos otra vez al abrir opciones.</summary>
[AddComponentMenu("")]
public sealed class CampusTextSizing : MonoBehaviour
{
    float authoredSize;
    public void Apply(TMP_Text text)
    {
        if (authoredSize <= 0) authoredSize = text.fontSize;
        text.fontSize = Mathf.Clamp(authoredSize * .85f, 20, 48);
    }
}
