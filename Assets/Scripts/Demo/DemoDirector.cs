using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Herramienta para GRABAR EL VIDEO DEMOSTRATIVO del Previo 2.
// Solo se activa cuando existe el archivo Grabacion/GRABAR.flag (lo crea el menú Taller3 > Grabar video demo).
// Recorre el juego automáticamente (menú, NPC, objetos, monedas, HUD) y guarda cada escena
// del video como una secuencia de cuadros JPG (formato MJPEG) a 30 fps en la carpeta Grabacion/.
public class DemoDirector : MonoBehaviour
{
    const int FPS = 30;
    const int OUT_W = 1280, OUT_H = 720;

    private string outDir;
    private FileStream stream;
    private string currentSegment;
    private int segmentFrames;
    private readonly List<string> log = new List<string>();
    private RenderTexture rt;
    private Texture2D frameTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Grabacion"));
        string flag = Path.Combine(dir, "GRABAR.flag");
        if (!File.Exists(flag)) return;
        File.Delete(flag);
        var go = new GameObject("DemoDirector");
        DontDestroyOnLoad(go);
        go.AddComponent<DemoDirector>().outDir = dir;
    }

    private IEnumerator Start()
    {
        Time.captureFramerate = FPS;   // tiempo fijo por cuadro: el video queda fluido aunque el PC vaya lento
        rt = new RenderTexture(OUT_W, OUT_H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);   // sin conversión de color (si no, el video sale lavado)
        frameTex = new Texture2D(OUT_W, OUT_H, TextureFormat.RGB24, false);
        StartCoroutine(CaptureLoop());
        yield return Run();
    }

    // ---------------------------------------------------------------- guion de la grabación
    private IEnumerator Run()
    {
        // 01. Menú principal y créditos
        Begin("01_menu");
        yield return Wait(4f);
        yield return Click("Btn_Credits");
        yield return Wait(9f);
        yield return Click("Btn_CloseCredits");
        yield return Wait(2f);
        yield return Click("Btn_Play");
        yield return WaitForScene("GameScene");
        End();

        // 02. HUD inicial, movimiento y colisión contra el muro (P1) + tecla E lejos (P2)
        Begin("02_hud_movimiento");
        yield return Wait(5f);
        GameInput.PressInteract();                 // P2: E lejos de todo -> no pasa nada
        yield return Wait(1.5f);
        yield return WalkTo(3f);
        yield return WalkTo(-14f, 2.0f);           // camina hasta el borde y empuja el muro (P1)
        yield return Wait(1f);
        yield return WalkTo(-4.6f);
        yield return Tap(-1f);                     // mira hacia Don Rafael
        End();

        // 03. Diálogo con Don Rafael (NPC)
        Begin("03_dialogo_npc");
        yield return Wait(2f);
        for (int i = 0; i < 6; i++) { GameInput.PressInteract(); yield return Wait(5.5f); }
        GameInput.PressInteract();                 // cierra el diálogo
        yield return Wait(2f);
        End();

        // 04. Primeras monedas
        Begin("04_monedas_1_2");
        yield return WalkTo(13.6f);
        yield return Wait(1.5f);
        End();

        // 05. Objeto interactivo 1: vitrina de la tienda
        Begin("05_vitrina");
        yield return Wait(1.5f);
        GameInput.PressInteract();
        yield return Wait(11f);
        GameInput.PressInteract();
        yield return Wait(1.5f);
        End();

        // 06. Tercera moneda y cartel del banco
        Begin("06_moneda_3");
        yield return WalkTo(26.2f);
        yield return Wait(1.5f);
        End();

        // 07. Objeto interactivo 2: cartel del banco
        Begin("07_cartel_banco");
        yield return Wait(1.5f);
        GameInput.PressInteract();
        yield return Wait(11f);
        GameInput.PressInteract();
        yield return Wait(1.5f);
        End();

        // 08. Monedas 4 y 5 -> misión cumplida + prueba de coleccionable único (P3)
        Begin("08_mision_cumplida");
        yield return WalkTo(43f);
        yield return Wait(4f);
        yield return WalkTo(38f);                  // vuelve a pasar por donde estaba una moneda (P3)
        yield return Wait(3f);
        End();

        // 09. Regreso con Don Rafael: conversación final
        Begin("09_dialogo_final");
        yield return WalkTo(-4.6f);
        yield return Tap(-1f);
        yield return Wait(1f);
        for (int i = 0; i < 3; i++) { GameInput.PressInteract(); yield return Wait(5f); }
        GameInput.PressInteract();
        yield return Wait(2f);
        End();

        // 10. Esc al menú y volver a jugar: todo se reinicia (P7)
        Begin("10_reinicio");
        GameInput.PressBack();
        yield return WaitForScene("MainMenu");
        yield return Wait(3f);
        yield return Click("Btn_Play");
        yield return WaitForScene("GameScene");
        yield return Wait(5f);
        End();

        File.WriteAllLines(Path.Combine(outDir, "segmentos.txt"), log);
        Time.captureFramerate = 0;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---------------------------------------------------------------- acciones
    private IEnumerator Wait(float seconds) { yield return new WaitForSeconds(seconds); }

    private IEnumerator WaitForScene(string scene)
    {
        while (SceneManager.GetActiveScene().name != scene) yield return null;
        yield return null;
    }

    private Transform Player()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.transform : null;
    }

    // Camina hasta la posición X (o hasta que pase "pushSeconds" empujando un muro)
    private IEnumerator WalkTo(float targetX, float pushSeconds = 0f)
    {
        var p = Player();
        if (p == null) yield break;
        float dir = Mathf.Sign(targetX - p.position.x);
        GameInput.SimulatedHorizontal = dir;
        float lastX = p.position.x, stuck = 0f, guard = 0f;
        while ((targetX - p.position.x) * dir > 0.08f && guard < 25f)
        {
            yield return null;
            guard += Time.deltaTime;
            if (Mathf.Abs(p.position.x - lastX) < 0.001f) stuck += Time.deltaTime; else stuck = 0f;
            lastX = p.position.x;
            if (pushSeconds > 0f && stuck >= pushSeconds) break;
        }
        GameInput.SimulatedHorizontal = 0f;
        yield return Wait(0.3f);
    }

    private IEnumerator Tap(float dir)
    {
        GameInput.SimulatedHorizontal = dir;
        yield return null; yield return null;
        GameInput.SimulatedHorizontal = 0f;
        yield return Wait(0.3f);
    }

    // "Clic" sobre un botón del menú: lo resalta un momento y ejecuta su On Click()
    private IEnumerator Click(string buttonName)
    {
        Button b = null;
        foreach (var btn in Resources.FindObjectsOfTypeAll<Button>())
            if (btn.name == buttonName && btn.gameObject.activeInHierarchy) { b = btn; break; }
        if (b == null) yield break;
        var img = b.targetGraphic;
        Color original = img.color;
        img.color = new Color32(255, 214, 170, 255);
        yield return Wait(0.35f);
        img.color = new Color32(196, 150, 120, 255);
        yield return Wait(0.2f);
        img.color = original;
        b.onClick.Invoke();
        yield return Wait(0.2f);
    }

    // ---------------------------------------------------------------- captura
    private void Begin(string segment)
    {
        End();
        currentSegment = segment;
        segmentFrames = 0;
        stream = new FileStream(Path.Combine(outDir, segment + ".mjpeg"), FileMode.Create, FileAccess.Write);
    }

    private void End()
    {
        if (stream == null) return;
        stream.Flush(); stream.Close(); stream = null;
        log.Add($"{currentSegment}\t{segmentFrames}\t{segmentFrames / (float)FPS:0.00}s");
    }

    private IEnumerator CaptureLoop()
    {
        var eof = new WaitForEndOfFrame();
        while (true)
        {
            yield return eof;
            if (stream == null) continue;
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            Graphics.Blit(shot, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            frameTex.ReadPixels(new Rect(0, 0, OUT_W, OUT_H), 0, 0);
            frameTex.Apply(false);
            RenderTexture.active = prev;
            Destroy(shot);
            byte[] jpg = frameTex.EncodeToJPG(88);
            stream.Write(jpg, 0, jpg.Length);
            segmentFrames++;
        }
    }

    private void OnDestroy()
    {
        End();
        Time.captureFramerate = 0;
        if (rt != null) rt.Release();
    }
}
