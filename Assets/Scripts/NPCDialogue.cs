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
// Cuando la misión del HUD está cumplida, el NPC usa la conversación final.
public class NPCDialogue : MonoBehaviour
{
    [Header("Secuencia de Conversación")]
    [SerializeField] private DialogueLine[] conversation;

    [Header("Conversación al completar la misión (opcional)")]
    [SerializeField] private DialogueLine[] missionCompleteConversation;

    [Header("Referencias de Interfaz (UI)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private GameObject interactionPrompt; // Icono flotante "E"

    private bool isPlayerInRange = false;
    private bool isDialogueActive = false;
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
        if (isPlayerInRange && GameInput.InteractDown)
        {
            if (!isDialogueActive) StartDialogue();
            else NextDialogueLine();
        }
    }

    private void StartDialogue()
    {
        bool missionDone = GameHUDManager.Instance != null && GameHUDManager.Instance.MissionCompleted;
        activeConversation = (missionDone && missionCompleteConversation != null && missionCompleteConversation.Length > 0)
            ? missionCompleteConversation : conversation;
        if (activeConversation == null || activeConversation.Length == 0) return;

        isDialogueActive = true;
        currentLineIndex = 0;
        GameInput.MovementLocked = true;               // Valentina se queda quieta mientras habla
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
        // 2. Color del nombre (si se definió)
        if (activeConversation[currentLineIndex].nameColor.a > 0f)
            nameText.color = activeConversation[currentLineIndex].nameColor;
        // 3. Texto de lo que está diciendo
        contentText.text = activeConversation[currentLineIndex].text;
    }

    private void EndDialogue()
    {
        bool wasActive = isDialogueActive;
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (wasActive) GameInput.MovementLocked = false;
        if (interactionPrompt != null) interactionPrompt.SetActive(isPlayerInRange);
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
            EndDialogue();
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
    }
}
