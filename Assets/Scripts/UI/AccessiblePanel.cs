using UnityEngine;
using UnityEngine.UI;

// Fondo accesible: en alto contraste los paneles se vuelven negros y opacos detrás del texto.
[RequireComponent(typeof(Graphic))]
public class AccessiblePanel : MonoBehaviour
{
    [SerializeField] private Color contrastColor = Color.black;

    private Graphic graphic;
    private Color baseColor;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseColor = graphic.color;
    }

    private void OnEnable() { GameSettings.OnChanged += Apply; Apply(); }
    private void OnDisable() { GameSettings.OnChanged -= Apply; }

    private void Apply()
    {
        if (graphic != null) graphic.color = GameSettings.HighContrast ? contrastColor : baseColor;
    }
}
