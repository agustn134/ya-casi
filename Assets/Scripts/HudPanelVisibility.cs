/* Ya Casi — Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Mantiene el fondo visible únicamente cuando su texto está visible. */
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class HudPanelVisibility : MonoBehaviour
{
    public TMP_Text target;
    private void LateUpdate()
    {
        bool visible = target != null && target.isActiveAndEnabled &&
            !string.IsNullOrWhiteSpace(target.text);
        // El objetivo ocupa una franja; los resultados necesitan dos líneas.
        if (target != null && target.name == "TextoObjetivo")
        {
            RectTransform panel = (RectTransform)transform;
            float height = target.text.Contains("\n") ? 156f : 88f;
            if (!Mathf.Approximately(panel.rect.height, height))
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
        foreach (Graphic graphic in GetComponents<Graphic>())
            graphic.enabled = visible;
    }
}
