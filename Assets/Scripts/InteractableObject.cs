using System.Collections;
using UnityEngine;
using TMPro;

// Pregunta de decisión financiera con varias opciones (una correcta)
[System.Serializable]
public class DecisionQuestion
{
    [TextArea(2, 4)] public string question;
    public string[] options = new string[3];
    public int correctIndex;
    [TextArea(2, 3)] public string whyCorrect;   // explicación corta del acierto
    [TextArea(2, 3)] public string whyWrong;     // explicación corta del error
}

// Objeto interactivo educativo (tecla E): muestra un indicador flotante al acercarse,
// abre un panel con un dato de educación financiera y, si tiene preguntas, plantea una decisión.
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

    [Header("Decisión (Momento 3)")]
    [SerializeField] private DecisionQuestion[] questions;
    [SerializeField] private GameObject solvedMark;         // chulo verde que aparece al resolverlo
    [SerializeField] private string placeName = "la tienda"; // para las pistas

    private bool isPlayerNearby;
    private bool isPanelOpen;
    private bool infoSeen;
    private int questionIndex;
    private int attempts;           // intentos en la pregunta actual
    private bool waiting;           // esperando la respuesta del panel de preguntas

    public bool HasQuestions => questions != null && questions.Length > 0;
    public bool Solved { get; private set; }
    public string PlaceName => placeName;

    private void Start()
    {
        if (promptFloatingText != null) promptFloatingText.SetActive(false);
        if (infoDisplayPanel != null) infoDisplayPanel.SetActive(false);
        if (solvedMark != null) solvedMark.SetActive(false);
    }

    private void Update()
    {
        if (!isPlayerNearby || waiting) return;
        if (LevelManager.Instance != null && LevelManager.Instance.Ended) return;
        if (PauseMenu.IsPaused) return;
        if (LevelManager.Instance != null && LevelManager.Instance.HasLives && LevelManager.Instance.Lives <= 0) return;
        if (GameInput.InteractDown) OnInteract();
    }

    private void OnInteract()
    {
        bool showInfo = LevelManager.Instance == null || LevelManager.Instance.Config.showInfoBeforeQuestion;

        if (isPanelOpen)                       // cerrar el dato educativo
        {
            SetInfo(false);
            if (HasQuestions && !Solved) AskCurrent();
            return;
        }

        GameEvents.Interact();
        if (HasQuestions && !Solved && (infoSeen || !showInfo)) { AskCurrent(); return; }
        SetInfo(true);
        infoSeen = true;
    }

    private void SetInfo(bool open)
    {
        isPanelOpen = open;
        GameInput.MovementLocked = open;          // Valentina se detiene a leer
        if (infoTitleComponent != null) infoTitleComponent.text = title;
        if (infoTextComponent != null)
            infoTextComponent.text = educationalContent + (open && HasQuestions && !Solved ? "\n\n<i>Presiona E para responder la pregunta.</i>" : "");
        if (infoDisplayPanel != null) infoDisplayPanel.SetActive(open);
        if (promptFloatingText != null) promptFloatingText.SetActive(!open && isPlayerNearby);
    }

    private void AskCurrent()
    {
        if (QuestionPanel.Instance == null) return;
        waiting = true;
        GameInput.MovementLocked = true;
        if (promptFloatingText != null) promptFloatingText.SetActive(false);
        string header = questions.Length > 1 ? $"{title}  ·  Pregunta {questionIndex + 1} de {questions.Length}" : title;
        QuestionPanel.Instance.Show(header, questions[questionIndex], OnAnswered);
    }

    private void OnAnswered(bool correct)
    {
        var q = questions[questionIndex];
        bool firstTry = attempts == 0;
        attempts++;
        int pts = LevelManager.Instance != null ? LevelManager.Instance.RegisterAnswer(correct, firstTry) : 0;

        if (correct)
        {
            if (EffectPool.Instance != null) EffectPool.Instance.CorrectBurst(transform.position + Vector3.up * 1.5f);
            FeedbackUI.Instance?.Show(FeedbackType.Acierto, "¡Buena decisión!" + (pts > 0 ? $"  +{pts}" : ""), q.whyCorrect, 4.5f);
            questionIndex++;
            attempts = 0;
            if (questionIndex < questions.Length) { StartCoroutine(NextQuestionSoon()); return; }
            Solved = true;
            if (solvedMark != null) solvedMark.SetActive(true);
            LevelManager.Instance?.DecisionSolved();
        }
        else
        {
            string extra = LevelManager.Instance != null && LevelManager.Instance.HasLives ? "  ·  -1 vida" : "";
            if (pts < 0) extra += $"  ·  {pts} pts";
            FeedbackUI.Instance?.Show(FeedbackType.Error, "Ojo con esa decisión" + extra, q.whyWrong + "\nPresiona E para intentarlo de nuevo.", 5.5f);
            FeedbackUI.Instance?.Shake();
            StartCoroutine(CheckLivesSoon());
        }
        waiting = false;
        GameInput.MovementLocked = false;
        if (promptFloatingText != null) promptFloatingText.SetActive(isPlayerNearby);
    }

    private IEnumerator NextQuestionSoon()
    {
        waiting = true;
        yield return new WaitForSeconds(1.2f);
        waiting = false;
        if (isPlayerNearby) AskCurrent();
        else GameInput.MovementLocked = false;
    }

    private IEnumerator CheckLivesSoon()
    {
        yield return new WaitForSeconds(1.6f);
        LevelManager.Instance?.CheckLivesAfterFeedback();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (promptFloatingText != null && !isPanelOpen && !waiting) promptFloatingText.SetActive(true);
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
