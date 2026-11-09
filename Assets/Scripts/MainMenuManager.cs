using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Administrador del menú principal de "El Primer Cheque".
// Se adjunta al GameObject vacío MenuManager de la escena MainMenu.
public class MainMenuManager : MonoBehaviour
{
    // El último nivel pide abrir los créditos al volver al menú
    public static bool OpenCreditsOnLoad;

    [Header("Paneles de Interfaz")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject firstButton;

    private void Start()
    {
        Time.timeScale = 1f;
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (OpenCreditsOnLoad) { OpenCreditsOnLoad = false; OpenCredits(); }
        else if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(MusicTrack.Menu);
        Select(firstButton);
    }

    // Carga el nivel 1
    public void PlayGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    // Muestra el panel con la información pedagógica y créditos (con su propia música)
    public void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(MusicTrack.Creditos);
    }

    // Oculta el panel de créditos
    public void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(MusicTrack.Menu);
        Select(firstButton);
    }

    public void OpenInstructions() { if (instructionsPanel != null) instructionsPanel.SetActive(true); }
    public void CloseInstructions() { if (instructionsPanel != null) instructionsPanel.SetActive(false); Select(firstButton); }
    public void OpenOptions() { if (optionsPanel != null) optionsPanel.SetActive(true); }
    public void CloseOptions() { if (optionsPanel != null) optionsPanel.SetActive(false); Select(firstButton); }

    private void Update()
    {
        // Esc cierra el panel que esté abierto
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (optionsPanel != null && optionsPanel.activeSelf) CloseOptions();
        else if (instructionsPanel != null && instructionsPanel.activeSelf) CloseInstructions();
        else if (creditsPanel != null && creditsPanel.activeSelf) CloseCredits();
    }

    private static void Select(GameObject go)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }

    // Cierra la aplicación
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
