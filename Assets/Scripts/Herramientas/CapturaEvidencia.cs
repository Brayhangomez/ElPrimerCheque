#if UNITY_EDITOR
using System.IO;
using UnityEngine;

// Herramienta solo del editor: con F12 guarda una captura del juego en la carpeta Capturas/
// (la usamos para las evidencias del informe). No se incluye en la build.
public class CapturaEvidencia : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        var go = new GameObject("CapturaEvidencia") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<CapturaEvidencia>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F12)) return;
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Capturas"));
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, System.DateTime.Now.ToString("HHmmss_fff") + ".png");
        ScreenCapture.CaptureScreenshot(file, 1);
        Debug.Log("Captura guardada: " + file);
    }
}
#endif
