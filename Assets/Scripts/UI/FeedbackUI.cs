using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum FeedbackType { Acierto, Error, Pista, Info }

// Retroalimentación visual (Actividad 2.2): mensaje corto con ícono + color + texto,
// para no depender solo del color (accesibilidad). En los errores la cámara vibra un poco
// (se puede desactivar en Opciones) y siempre se explica el motivo en una línea.
public class FeedbackUI : MonoBehaviour
{
    public static FeedbackUI Instance { get; private set; }

    [SerializeField] private CanvasGroup toast;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Sprite correctIcon, wrongIcon, hintIcon, infoIcon;
    [SerializeField] private Color correctColor = new Color32(62, 120, 60, 245);
    [SerializeField] private Color wrongColor = new Color32(150, 50, 42, 245);
    [SerializeField] private Color hintColor = new Color32(60, 80, 130, 245);
    [SerializeField] private Color infoColor = new Color32(42, 30, 26, 245);

    private Coroutine running;
    private RectTransform rt;
    private Vector2 basePos;

    private void Awake()
    {
        Instance = this;
        rt = (RectTransform)toast.transform;
        basePos = rt.anchoredPosition;
        toast.alpha = 0f;
        toast.gameObject.SetActive(false);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Show(FeedbackType type, string title, string body, float seconds = 4f)
    {
        background.color = type switch
        {
            FeedbackType.Acierto => correctColor,
            FeedbackType.Error => wrongColor,
            FeedbackType.Pista => hintColor,
            _ => infoColor
        };
        if (GameSettings.HighContrast) background.color = Color.black;
        icon.sprite = type switch
        {
            FeedbackType.Acierto => correctIcon,
            FeedbackType.Error => wrongIcon,
            FeedbackType.Pista => hintIcon,
            _ => infoIcon
        };
        titleText.text = title;
        bodyText.text = body;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Animate(seconds));
    }

    private IEnumerator Animate(float seconds)
    {
        toast.gameObject.SetActive(true);
        // entra deslizándose desde arriba
        for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.22f;
            toast.alpha = k;
            rt.anchoredPosition = basePos + Vector2.up * (40f * (1f - k));
            yield return null;
        }
        toast.alpha = 1f; rt.anchoredPosition = basePos;
        yield return new WaitForSecondsRealtime(seconds);
        for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            toast.alpha = 1f - t / 0.35f;
            yield return null;
        }
        toast.gameObject.SetActive(false);
    }

    // Vibración corta de la cámara en los errores (desactivable en Opciones)
    public void Shake(float seconds = 0.25f, float strength = 0.12f)
    {
        if (!GameSettings.CameraShake) return;
        CameraFollow2D.Shake(seconds, strength);
    }
}
