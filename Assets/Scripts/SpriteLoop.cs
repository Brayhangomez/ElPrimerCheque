using UnityEngine;

// Animación sencilla por cuadros para objetos decorativos (moneda girando, NPC respirando).
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 6f;
    [Header("Flotación (opcional)")]
    [SerializeField] private float bobAmplitude = 0f;
    [SerializeField] private float bobSpeed = 2f;

    private SpriteRenderer sr;
    private Vector3 basePos;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        basePos = transform.localPosition;
    }

    private void Update()
    {
        if (frames != null && frames.Length > 0)
            sr.sprite = frames[(int)(Time.time * framesPerSecond) % frames.Length];
        if (bobAmplitude > 0f)
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
    }
}
