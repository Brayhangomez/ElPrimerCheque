using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Texto accesible: aumenta el tamaño cuando el jugador activa "Texto grande" (Actividad 2.4)
// y cambia a blanco/amarillo sobre negro con "Alto contraste".
[RequireComponent(typeof(TMP_Text))]
public class AccessibleText : MonoBehaviour
{
    [SerializeField] private float largeScale = 1.2f;
    [SerializeField] private bool accent;          // títulos y nombres: amarillo en alto contraste

    private TMP_Text text;
    private float baseSize;
    private Color baseColor;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        baseSize = text.fontSize;
        baseColor = text.color;
    }

    private void OnEnable() { GameSettings.OnChanged += Apply; Apply(); }
    private void OnDisable() { GameSettings.OnChanged -= Apply; }

    private void Apply()
    {
        if (text == null) return;
        bool large = GameSettings.LargeText;
        // con texto grande se activa el auto-ajuste para que el texto no se salga de su caja
        text.enableAutoSizing = large;
        text.fontSizeMax = baseSize * largeScale;
        text.fontSizeMin = baseSize * 0.8f;
        text.fontSize = baseSize * (large ? largeScale : 1f);
        text.color = GameSettings.HighContrast ? (accent ? new Color32(255, 214, 0, 255) : Color.white) : baseColor;
    }
}
