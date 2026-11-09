using UnityEngine;

// Moneda de ahorro coleccionable: se recoge al pasar por encima (trigger),
// suma una sola vez, lanza el evento de recolección (sonido + partículas) y desaparece.
public class CollectibleItem : MonoBehaviour
{
    [Header("Identificación")]
    [SerializeField] private string itemName = "Moneda de ahorro";
    [SerializeField] private int itemValue = 1;

    private bool isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Validación: solo el Player y solo una vez (Prueba P3)
        if (collision.CompareTag("Player") && !isCollected)
        {
            isCollected = true;

            // Notificar a las reglas del nivel (o al HUD si la escena no tiene LevelManager)
            if (LevelManager.Instance != null) LevelManager.Instance.AddCoin(itemValue);
            else if (GameHUDManager.Instance != null) GameHUDManager.Instance.AddCollectible(itemValue, itemName);

            // Evento: el AudioManager suena la moneda; el pool muestra las partículas
            GameEvents.ItemCollected(itemValue);
            if (EffectPool.Instance != null) EffectPool.Instance.CoinBurst(transform.position);

            // Desaparecer el objeto de la escena
            Destroy(gameObject);
        }
    }
}
