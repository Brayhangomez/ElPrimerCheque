using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Administrador del HUD: misión actual, contador de monedas, mini-inventario con iconos
// y panel de misión cumplida. Esc vuelve al menú principal.
public class GameHUDManager : MonoBehaviour
{
    public static GameHUDManager Instance { get; private set; }

    [Header("Referencias de Texto HUD")]
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private GameObject missionCompletePanel;

    [Header("Mini-inventario")]
    [SerializeField] private Image[] inventorySlots;
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite filledSlotSprite;

    [Header("Configuración de Meta")]
    [SerializeField] private int totalRequired = 5;
    [SerializeField] private string objectiveMessage = "Misión: ahorra 5 monedas para tu fondo de emergencia";
    [SerializeField] private string completedObjectiveMessage = "Misión cumplida: vuelve a hablar con Don Rafael";
    [SerializeField] private float missionPanelSeconds = 6f;

    private int currentCollected = 0;
    private float panelTimer = -1f;

    public bool MissionCompleted { get; private set; }
    public int CurrentCollected => currentCollected;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (objectiveText != null) objectiveText.text = objectiveMessage;
        UpdateHUDVisuals();
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
    }

    private void Update()
    {
        // El panel de misión cumplida se cierra solo después de unos segundos
        if (panelTimer > 0f)
        {
            panelTimer -= Time.deltaTime;
            if (panelTimer <= 0f && missionCompletePanel != null) missionCompletePanel.SetActive(false);
        }

        // Esc: volver al menú principal (al volver a jugar todo se reinicia, Prueba P7)
        if (GameInput.BackDown)
        {
            GameInput.ResetState();
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void AddCollectible(int amount, string itemName = "")
    {
        if (MissionCompleted) return;
        currentCollected = Mathf.Min(currentCollected + amount, totalRequired);
        UpdateHUDVisuals();
        if (currentCollected >= totalRequired) OnMissionCompleted();
    }

    private void UpdateHUDVisuals()
    {
        if (counterText != null)
            counterText.text = $"Ahorro: {currentCollected} / {totalRequired}";

        if (inventorySlots != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] == null) continue;
                bool filled = i < currentCollected;
                inventorySlots[i].sprite = filled ? filledSlotSprite : emptySlotSprite;
            }
        }
    }

    private void OnMissionCompleted()
    {
        MissionCompleted = true;
        if (objectiveText != null) objectiveText.text = completedObjectiveMessage;
        if (missionCompletePanel != null)
        {
            missionCompletePanel.SetActive(true);
            panelTimer = missionPanelSeconds;
        }
    }
}
