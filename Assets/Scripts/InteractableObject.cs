using UnityEngine;
using TMPro;

// Objeto interactivo educativo (tecla E): muestra un indicador flotante al acercarse
// y abre un panel con un dato de educación financiera.
public class InteractableObject : MonoBehaviour
{
    [Header("Mensaje Flotante")]
    [SerializeField] private GameObject promptFloatingText; // Indicador "E" sobre el objeto

    [Header("Contenido Educativo")]
    [SerializeField] private GameObject infoDisplayPanel;
    [SerializeField] private TMP_Text infoTitleComponent;
    [SerializeField] private TMP_Text infoTextComponent;
    [SerializeField] private string title = "Dato financiero";
    [TextArea(3, 6)]
    [SerializeField] private string educationalContent = "La tarjeta de crédito no es dinero extra: es una deuda.";

    private bool isPlayerNearby = false;
    private bool isPanelOpen = false;

    private void Start()
    {
        if (promptFloatingText != null) promptFloatingText.SetActive(false);
        if (infoDisplayPanel != null) infoDisplayPanel.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerNearby && GameInput.InteractDown) ToggleInteraction();
    }

    private void ToggleInteraction()
    {
        isPanelOpen = !isPanelOpen;
        GameInput.MovementLocked = isPanelOpen;          // Valentina se detiene a leer
        if (infoTitleComponent != null) infoTitleComponent.text = title;
        if (infoTextComponent != null) infoTextComponent.text = educationalContent;
        if (infoDisplayPanel != null) infoDisplayPanel.SetActive(isPanelOpen);
        if (promptFloatingText != null) promptFloatingText.SetActive(!isPanelOpen);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (promptFloatingText != null && !isPanelOpen) promptFloatingText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (isPanelOpen) GameInput.MovementLocked = false;
            isPanelOpen = false;
            if (promptFloatingText != null) promptFloatingText.SetActive(false);
            if (infoDisplayPanel != null) infoDisplayPanel.SetActive(false);
        }
    }
}
