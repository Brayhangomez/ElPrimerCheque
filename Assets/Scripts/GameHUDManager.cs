using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD del nivel: capítulo, objetivos, monedas con mini-inventario, barra de progreso,
// vidas, tiempo y puntaje. Solo muestra datos: las reglas están en el LevelManager.
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

    [Header("Configuración de Meta (solo si no hay LevelManager)")]
    [SerializeField] private int totalRequired = 5;
    [SerializeField] private string objectiveMessage = "Misión: ahorra 5 monedas para tu fondo de emergencia";
    [SerializeField] private string completedObjectiveMessage = "Vuelve a hablar con Don Rafael";
    [SerializeField] private float missionPanelSeconds = 5f;

    [Header("Momento 3: indicadores")]
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject livesRoot;
    [SerializeField] private Image[] hearts;
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;

    private int shownCoins = -1;
    private int shownLives = -1;
    private float panelTimer = -1f;
    private bool readyShown;
    private float progressShown;
    private Color timerBaseColor;

    public bool MissionCompleted => LevelManager.Instance != null ? LevelManager.Instance.MissionReady : false;
    public int CurrentCollected => LevelManager.Instance != null ? LevelManager.Instance.Coins : 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        if (timerText != null) timerBaseColor = timerText.color;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (LevelManager.Instance != null) LevelManager.Instance.OnStateChanged -= Refresh;
    }

    private void Start()
    {
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
        var lm = LevelManager.Instance;
        if (lm == null)
        {
            if (objectiveText != null) objectiveText.text = objectiveMessage;
            if (counterText != null) counterText.text = $"Ahorro: 0 / {totalRequired}";
            return;
        }
        lm.OnStateChanged += Refresh;
        if (chapterText != null) chapterText.text = lm.Config.chapterTitle;
        // solo se muestran los espacios del inventario que pide este nivel
        for (int i = 0; i < inventorySlots.Length; i++)
            if (inventorySlots[i] != null) inventorySlots[i].gameObject.SetActive(i < lm.CoinsRequired);
        if (livesRoot != null) livesRoot.SetActive(lm.HasLives);
        if (timerRoot != null) timerRoot.SetActive(lm.HasTimer);
        Refresh();
    }

    // Compatibilidad con el Previo 2 (las monedas avisan al LevelManager)
    public void AddCollectible(int amount, string itemName = "")
    {
        if (LevelManager.Instance != null) LevelManager.Instance.AddCoin(amount);
    }

    private void Update()
    {
        if (panelTimer > 0f)
        {
            panelTimer -= Time.deltaTime;
            if (panelTimer <= 0f && missionCompletePanel != null) missionCompletePanel.SetActive(false);
        }
        // la barra de progreso se llena de forma suave
        var lm = LevelManager.Instance;
        if (lm != null && progressFill != null)
        {
            progressShown = Mathf.MoveTowards(progressShown, lm.Progress, Time.unscaledDeltaTime * 0.8f);
            progressFill.fillAmount = progressShown;
        }
        // el reloj late cuando quedan menos de 20 segundos
        if (lm != null && lm.HasTimer && timerText != null && !lm.Ended && lm.TimeLeft < 20f)
            timerText.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)));
    }

    private void Refresh()
    {
        var lm = LevelManager.Instance;
        if (lm == null) return;

        if (counterText != null) counterText.text = $"Ahorro: {lm.Coins} / {lm.CoinsRequired}";
        if (shownCoins != lm.Coins)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] == null) continue;
                bool filled = i < lm.Coins;
                inventorySlots[i].sprite = filled ? filledSlotSprite : emptySlotSprite;
                if (shownCoins >= 0 && filled && i >= shownCoins) StartCoroutine(Punch(inventorySlots[i].transform));
            }
            shownCoins = lm.Coins;
        }

        if (objectiveText != null)
        {
            string ok = "<color=#4E7A3A><b>√</b></color>";
            string coins = lm.Coins >= lm.CoinsRequired ? $"{ok} Monedas de ahorro: {lm.CoinsRequired}/{lm.CoinsRequired}" : $"○ Monedas de ahorro: {lm.Coins}/{lm.CoinsRequired}";
            string dec = lm.DecisionsRequired == 0 ? "" :
                (lm.DecisionsSolved >= lm.DecisionsRequired ? $"\n{ok} Decisiones: {lm.DecisionsRequired}/{lm.DecisionsRequired}" : $"\n○ Decisiones correctas: {lm.DecisionsSolved}/{lm.DecisionsRequired}");
            string talk = lm.MissionReady && lm.Config.requireFinalTalk ? $"\n<b>► {completedObjectiveMessage}</b>" : "";
            objectiveText.text = coins + dec + talk;
        }

        if (progressText != null) progressText.text = $"{Mathf.RoundToInt(lm.Progress * 100f)}%";

        if (hearts != null && lm.HasLives && shownLives != lm.Lives)
        {
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                hearts[i].gameObject.SetActive(i < lm.Config.lives);
                hearts[i].sprite = i < lm.Lives ? heartFull : heartEmpty;
                if (shownLives > 0 && i == lm.Lives) StartCoroutine(Punch(hearts[i].transform));
            }
            shownLives = lm.Lives;
        }

        if (timerText != null && lm.HasTimer)
        {
            int s = Mathf.CeilToInt(lm.TimeLeft);
            timerText.text = $"{s / 60}:{s % 60:00}";
            timerText.color = s <= 20 ? new Color32(205, 70, 60, 255) : (GameSettings.HighContrast ? Color.white : timerBaseColor);
        }

        if (scoreText != null) scoreText.text = $"Puntos: {lm.Score}";

        if (lm.MissionReady && !readyShown && lm.Config.requireFinalTalk && !lm.Ended)
        {
            readyShown = true;
            if (missionCompletePanel != null) { missionCompletePanel.SetActive(true); panelTimer = missionPanelSeconds; }
        }
    }

    // Pequeño "salto" de escala para llamar la atención sobre un cambio del HUD
    private IEnumerator Punch(Transform t)
    {
        for (float k = 0; k < 0.25f; k += Time.unscaledDeltaTime)
        {
            t.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k / 0.25f * Mathf.PI));
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}
