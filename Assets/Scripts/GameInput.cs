using UnityEngine;

// Capa de entrada del juego: lee el teclado (A/D, flechas, E, Esc) en un solo lugar.
// También permite bloquear el movimiento mientras hay un diálogo o un panel abierto.
public static class GameInput
{
    public static bool MovementLocked;      // true mientras se lee un diálogo o un cartel

    // Entradas simuladas (solo las usa el DemoDirector para grabar el video demostrativo)
    public static float SimulatedHorizontal;
    private static int simInteractFrame = -1;
    private static int simBackFrame = -1;

    public static float Horizontal
    {
        get
        {
            if (MovementLocked) return 0f;
            float h = Input.GetAxisRaw("Horizontal");
            return Mathf.Abs(h) > 0.01f ? h : SimulatedHorizontal;
        }
    }

    public static float Vertical => MovementLocked ? 0f : Input.GetAxisRaw("Vertical");

    // Tecla E: hablar / interactuar / avanzar diálogo
    public static bool InteractDown => Input.GetKeyDown(KeyCode.E) || Time.frameCount == simInteractFrame;

    // Tecla Esc: volver al menú principal
    public static bool BackDown => Input.GetKeyDown(KeyCode.Escape) || Time.frameCount == simBackFrame;

    public static void PressInteract() { simInteractFrame = Time.frameCount + 1; }
    public static void PressBack() { simBackFrame = Time.frameCount + 1; }

    public static void ResetState()
    {
        MovementLocked = false;
        SimulatedHorizontal = 0f;
    }
}
