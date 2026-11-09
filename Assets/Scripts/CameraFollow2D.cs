using UnityEngine;

// Cámara con zona muerta y límites de escenario (clamping).
// Momento 3: vibración corta para la retroalimentación de error (se puede apagar en Opciones).
public class CameraFollow2D : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform target;

    [Header("Configuración de Suavizado")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Zona Muerta (Dead Zone)")]
    [SerializeField] private float deadZoneX = 1.5f;

    [Header("Límites del Escenario (Clamping)")]
    [SerializeField] private bool useLimits = true;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 25f;

    private static float shakeTime, shakeStrength;
    private Vector3 basePos;
    private bool init;

    public static void Shake(float seconds, float strength)
    {
        shakeTime = seconds; shakeStrength = strength;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (!init) { basePos = transform.position; init = true; }

        Vector3 targetPosition = basePos;
        float deltaX = target.position.x - basePos.x;

        // Evaluación de la Zona Muerta en el eje X
        if (Mathf.Abs(deltaX) > deadZoneX)
        {
            targetPosition.x = target.position.x - (Mathf.Sign(deltaX) * deadZoneX);
        }

        targetPosition.y = target.position.y + offset.y;
        targetPosition.z = offset.z;

        // Aplicación de Restricciones (Clamping)
        if (useLimits)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        }

        basePos = Vector3.Lerp(basePos, targetPosition, smoothSpeed * Time.deltaTime);

        Vector3 shake = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            shake = (Vector3)(Random.insideUnitCircle * shakeStrength);
        }
        transform.position = basePos + shake;
    }

    private void OnDrawGizmosSelected()
    {
        // Dibuja guías visuales en la Scene View
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneX * 2, 2f, 0f));

        if (useLimits)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(minX, transform.position.y - 5, 0), new Vector3(minX, transform.position.y + 5, 0));
            Gizmos.DrawLine(new Vector3(maxX, transform.position.y - 5, 0), new Vector3(maxX, transform.position.y + 5, 0));
        }
    }
}
