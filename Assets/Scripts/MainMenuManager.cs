using UnityEngine;
using UnityEngine.SceneManagement;

// Administrador del menú principal de "El Primer Cheque".
// Se adjunta al GameObject vacío MenuManager de la escena MainMenu.
public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles de Interfaz")]
    [SerializeField] private GameObject creditsPanel;

    // Carga la escena del juego
    public void PlayGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    // Muestra el panel con la información pedagógica y créditos
    public void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    // Oculta el panel de créditos
    public void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
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
