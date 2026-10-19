using UnityEngine;

// Moneda de ahorro coleccionable: se recoge al pasar por encima (trigger),
// suma al HUD una sola vez y desaparece de la escena.
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

            // Notificar al gestor del HUD
            if (GameHUDManager.Instance != null)
                GameHUDManager.Instance.AddCollectible(itemValue, itemName);

            // Desaparecer el objeto de la escena
            Destroy(gameObject);
        }
    }
}
