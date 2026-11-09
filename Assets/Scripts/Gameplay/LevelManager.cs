using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Reglas de cada nivel: objetivos, tiempo, vidas, pistas, puntaje, victoria y derrota.
// Los valores de dificultad vienen del LevelConfig (ScriptableObject) asignado en el Inspector.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelConfig config;

    [Header("Paneles de fin de nivel")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text victoryTitle;
    [SerializeField] private TMP_Text victoryBody;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject creditsButton;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private TMP_Text defeatBody;

    [Header("Escena")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform npc;

    public event Action OnStateChanged;

    public LevelConfig Config => config;
    public int Coins { get; private set; }
    public int CoinsRequired => config.coinsRequired;
    public int DecisionsSolved { get; private set; }
    public int DecisionsRequired { get; private set; }
    public int Lives { get; private set; }
    public float TimeLeft { get; private set; }
    public int Score { get; private set; }
    public int HintsUsed { get; private set; }
    public int CorrectAnswers { get; private set; }
    public int WrongAnswers { get; private set; }
    public bool TalkedToNpc { get; private set; }
    public bool Ended { get; private set; }
    public bool Won { get; private set; }

    public bool MissionReady => Coins >= CoinsRequired && DecisionsSolved >= DecisionsRequired;
    public bool HasTimer => config.timeLimit > 0f;
    public bool HasLives => config.lives > 0;
    public int HintsLeft => config.hintsAvailable < 0 ? int.MaxValue : Mathf.Max(0, config.hintsAvailable - HintsUsed);
    public Transform Player => player;
    public Transform Npc => npc;

    // Progreso de 0 a 1 para la barra del HUD
    public float Progress
    {
        get
        {
            float total = CoinsRequired + DecisionsRequired + (config.requireFinalTalk ? 1 : 0);
            float done = Mathf.Min(Coins, CoinsRequired) + DecisionsSolved + (Won ? 1 : 0);
            return total <= 0 ? 1f : done / total;
        }
    }

    private readonly List<InteractableObject> decisionPoints = new List<InteractableObject>();
    private bool readyAnnounced;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Lives = config.lives;
        TimeLeft = config.timeLimit;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start()
    {
        // Cuenta cuántos puntos de decisión (objetos con preguntas) tiene el nivel
        foreach (var io in FindObjectsByType<InteractableObject>(FindObjectsSortMode.None))
            if (io.HasQuestions) decisionPoints.Add(io);
        DecisionsRequired = decisionPoints.Count;
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(config.music);
        Changed();
    }

    private void Update()
    {
        // El reloj no corre mientras se lee un diálogo, un cartel o una pregunta (evita castigos injustos)
        if (Ended || !HasTimer || GameInput.MovementLocked) return;
        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            Lose("Se acabó el tiempo: el mes se fue más rápido que la plata.");
        }
        Changed();
    }

    private void Changed() => OnStateChanged?.Invoke();

    // ---------------------------------------------------------------- objetivos
    public void AddCoin(int amount)
    {
        if (Ended) return;
        Coins += amount;
        Score += amount * config.pointsPerCoin;
        CheckReady();
        Changed();
    }

    public void NotifyTalked() { TalkedToNpc = true; Changed(); }

    public void DecisionSolved()
    {
        DecisionsSolved = Mathf.Min(DecisionsSolved + 1, DecisionsRequired);
        CheckReady();
        Changed();
    }

    public InteractableObject NextPendingDecision(Vector3 from)
    {
        InteractableObject best = null; float bd = float.MaxValue;
        foreach (var d in decisionPoints)
        {
            if (d == null || d.Solved) continue;
            float dist = Mathf.Abs(d.transform.position.x - from.x);
            if (dist < bd) { bd = dist; best = d; }
        }
        return best;
    }

    private void CheckReady()
    {
        if (!MissionReady || readyAnnounced) return;
        readyAnnounced = true;
        // con charla final, el aviso lo muestra el panel "¡Objetivos cumplidos!" del HUD
        // (antes salía también un mensaje encima y se tapaban; ver registro de bugs)
        if (!config.requireFinalTalk) CompleteLevel();
    }

    // ---------------------------------------------------------------- respuestas
    // Devuelve los puntos ganados o perdidos para mostrarlos en la retroalimentación
    public int RegisterAnswer(bool correct, bool firstTry)
    {
        if (Ended) return 0;
        int pts;
        if (correct)
        {
            CorrectAnswers++;
            pts = firstTry ? config.pointsCorrectFirstTry : config.pointsCorrectRetry;
            Score += pts;
            GameEvents.CorrectAnswer();
        }
        else
        {
            WrongAnswers++;
            pts = -config.wrongAnswerPenalty;
            Score = Mathf.Max(0, Score + pts);
            if (HasLives) Lives = Mathf.Max(0, Lives - 1);
            GameEvents.WrongAnswer();
        }
        Changed();
        return pts;
    }

    // Se llama después de mostrar la explicación del error, para no tapar el mensaje con la derrota
    public void CheckLivesAfterFeedback()
    {
        if (HasLives && Lives <= 0 && !Ended)
            Lose("Te quedaste sin vidas: tomaste demasiadas malas decisiones con tu plata.");
    }

    // ---------------------------------------------------------------- pistas
    public bool TryUseHint()
    {
        if (Ended || HintsLeft <= 0) return false;
        HintsUsed++;
        Score = Mathf.Max(0, Score - config.hintCost);
        GameEvents.HintUsed();
        Changed();
        return true;
    }

    // ---------------------------------------------------------------- fin del nivel
    public void CompleteLevel()
    {
        if (Ended) return;
        Ended = true; Won = true;
        int timeBonus = HasTimer ? Mathf.RoundToInt(TimeLeft) * config.pointsPerSecondLeft : 0;
        Score += timeBonus;
        GameInput.MovementLocked = true;
        PlayerPrefs.SetInt("nivel_maximo", Mathf.Max(PlayerPrefs.GetInt("nivel_maximo", 1), config.levelNumber + 1));
        PlayerPrefs.Save();
        GameEvents.LevelCompleted(config.levelNumber);
        if (EffectPool.Instance != null && player != null) EffectPool.Instance.Confetti(player.position + Vector3.up * 2f);
        bool last = string.IsNullOrEmpty(config.nextScene);
        if (victoryTitle != null) victoryTitle.text = last ? "¡Sobreviviste a tu primer mes!" : "¡Capítulo completado!";
        if (victoryBody != null)
        {
            string resumen =
                $"Monedas de ahorro: {Coins}/{CoinsRequired}\n" +
                $"Decisiones correctas: {CorrectAnswers}   ·   Errores: {WrongAnswers}\n" +
                $"Pistas usadas: {HintsUsed}" + (HasTimer ? $"   ·   Bono por tiempo: +{timeBonus}" : "") +
                $"\n\n<size=130%><b>Puntaje: {Score}</b></size>";
            victoryBody.text = resumen;
        }
        if (nextButton != null) nextButton.SetActive(!last);
        if (creditsButton != null) creditsButton.SetActive(last);
        if (victoryPanel != null) victoryPanel.SetActive(true);
        Changed();
    }

    private void Lose(string reason)
    {
        if (Ended) return;
        Ended = true; Won = false;
        GameInput.MovementLocked = true;
        GameEvents.Defeat();
        if (defeatBody != null) defeatBody.text = reason + "\n\nNo pasa nada: revisa la pista y vuelve a intentarlo.";
        if (defeatPanel != null) defeatPanel.SetActive(true);
        Changed();
    }

    // ---------------------------------------------------------------- navegación (botones)
    public void LoadNext()
    {
        Time.timeScale = 1f; GameInput.ResetState();
        SceneManager.LoadScene(config.nextScene);
    }

    public void Retry()
    {
        Time.timeScale = 1f; GameInput.ResetState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f; GameInput.ResetState();
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToCredits()
    {
        MainMenuManager.OpenCreditsOnLoad = true;
        GoToMenu();
    }
}
