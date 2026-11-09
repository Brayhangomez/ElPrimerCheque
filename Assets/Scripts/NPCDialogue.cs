using UnityEngine;
using TMPro;

// Estructura que une el nombre de quien habla con su texto
[System.Serializable]
public struct DialogueLine
{
    [Tooltip("Nombre de quien habla: ej. 'Don Rafael' o 'Valentina'")]
    public string speakerName;

    [Tooltip("Color opcional para distinguir el nombre de quien habla")]
    public Color nameColor;

    [TextArea(2, 5)]
    public string text;
}

// Sistema de diálogo con NPC (tecla E). Diálogo compartido Jugador <-> NPC.
// Cuando los objetivos del nivel están cumplidos, el NPC usa la conversación final y cierra el capítulo.
public class NPCDialogue : MonoBehaviour
{
    [Header("Secuencia de Conversación")]
    [SerializeField] private DialogueLine[] conversation;

    [Header("Conversación al completar la misión (opcional)")]
    [SerializeField] private DialogueLine[] missionCompleteConversation;

    [Header("Conversación mientras faltan objetivos (opcional)")]
    [SerializeField] private DialogueLine[] reminderConversation;

    [Header("Referencias de Interfaz (UI)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private GameObject interactionPrompt; // Icono flotante "E"

    private bool isPlayerInRange = false;
    private bool isDialogueActive = false;
    private bool talkedOnce = false;
    private bool finalActive = false;
    private int currentLineIndex = 0;
    private DialogueLine[] activeConversation;
    private PlayerController player;

    public bool IsDialogueActive => isDialogueActive;

    private void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (PauseMenu.IsPaused) return;
        if (LevelManager.Instance != null && LevelManager.Instance.Ended) return;
        if (isPlayerInRange && GameInput.InteractDown)
        {
            if (!isDialogueActive) StartDialogue();
            else NextDialogueLine();
        }
    }

    private void StartDialogue()
    {
        var lm = LevelManager.Instance;
        bool missionDone = lm != null ? lm.MissionReady : (GameHUDManager.Instance != null && GameHUDManager.Instance.MissionCompleted);
        finalActive = missionDone && missionCompleteConversation != null && missionCompleteConversation.Length > 0;
        if (finalActive) activeConversation = missionCompleteConversation;
        else if (talkedOnce && reminderConversation != null && reminderConversation.Length > 0) activeConversation = reminderConversation;
        else activeConversation = conversation;
        if (activeConversation == null || activeConversation.Length == 0) return;

        isDialogueActive = true;
        currentLineIndex = 0;
        GameInput.MovementLocked = true;               // Valentina se queda quieta mientras habla
        GameEvents.Interact();
        if (player != null) player.FaceTowards(transform.position.x);
        dialoguePanel.SetActive(true);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        DisplayCurrentLine();
    }

    private void NextDialogueLine()
    {
        currentLineIndex++;
        if (currentLineIndex < activeConversation.Length) DisplayCurrentLine();
        else EndDialogue();
    }

    private void DisplayCurrentLine()
    {
        // 1. Nombre de quien habla
        nameText.text = activeConversation[currentLineIndex].speakerName;
        // 2. Color del nombre (si se definió; en alto contraste se deja el amarillo de accesibilidad)
        if (activeConversation[currentLineIndex].nameColor.a > 0f && !GameSettings.HighContrast)
            nameText.color = activeConversation[currentLineIndex].nameColor;
        // 3. Texto de lo que está diciendo
        contentText.text = activeConversation[currentLineIndex].text;
        GameEvents.DialogueLine();
    }

    private void EndDialogue()
    {
        bool wasActive = isDialogueActive;
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (wasActive) GameInput.MovementLocked = false;
        if (interactionPrompt != null) interactionPrompt.SetActive(isPlayerInRange);
        if (!wasActive) return;
        if (!talkedOnce)
        {
            talkedOnce = true;
            if (LevelManager.Instance != null) LevelManager.Instance.NotifyTalked();
        }
        // la conversación final cierra el capítulo
        if (finalActive && LevelManager.Instance != null) LevelManager.Instance.CompleteLevel();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            player = collision.GetComponent<PlayerController>();
            if (!isDialogueActive && interactionPrompt != null) interactionPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (isDialogueActive) { finalActive = false; EndDialogue(); }
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
    }
}
