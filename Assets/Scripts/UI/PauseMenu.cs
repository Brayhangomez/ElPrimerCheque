using UnityEngine;
using UnityEngine.EventSystems;

// Menú de pausa (Esc o P): continuar, reiniciar, opciones y volver al menú.
// Permite pausar en cualquier momento (Actividad 2.4, ritmo).
public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject firstButton;

    private bool lockBefore;

    private void Awake() { IsPaused = false; }

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    private void OnDestroy() { IsPaused = false; Time.timeScale = 1f; }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (LevelManager.Instance != null && LevelManager.Instance.Ended) return;
            if (optionsPanel != null && optionsPanel.activeSelf) { CloseOptions(); return; }
            if (QuestionPanel.Instance != null && QuestionPanel.Instance.IsOpen && !IsPaused) return;
            if (IsPaused) Resume(); else Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        lockBefore = GameInput.MovementLocked;
        GameInput.MovementLocked = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        Select(firstButton);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        GameInput.MovementLocked = lockBefore;
        pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Select(null);
    }

    public void OpenOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
        Select(firstButton);
    }

    public void RestartLevel() { IsPaused = false; LevelManager.Instance?.Retry(); }
    public void BackToMenu() { IsPaused = false; LevelManager.Instance?.GoToMenu(); }

    private static void Select(GameObject go)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }
}
