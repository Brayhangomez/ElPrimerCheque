using System.Collections;
using UnityEngine;

// Animación de aparición de paneles: crecen un poco y aparecen con un fundido (Actividad 1.3, UI).
// Usa tiempo sin escala para que funcione también con el juego en pausa.
[RequireComponent(typeof(CanvasGroup))]
public class UIPop : MonoBehaviour
{
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float startScale = 0.88f;
    [SerializeField] private bool playSound = true;

    private CanvasGroup group;

    private void Awake() { group = GetComponent<CanvasGroup>(); }

    private void OnEnable()
    {
        if (playSound) GameEvents.PanelOpened();
        StartCoroutine(Pop());
    }

    private IEnumerator Pop()
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            float e = 1f - (1f - k) * (1f - k);           // ease-out
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, e);
            group.alpha = k;
            yield return null;
        }
        transform.localScale = Vector3.one;
        group.alpha = 1f;
    }
}
