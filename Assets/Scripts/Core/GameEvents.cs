using System;

// Bus de eventos sencillo del juego (Momento 3, Actividad 1.3.1).
// El script que detecta una acción lanza el evento; el audio, las animaciones y la UI se suscriben.
// Así el sonido y el movimiento quedan sincronizados sin que los scripts se conozcan entre sí.
public static class GameEvents
{
    public static event Action<int> OnItemCollected;     // puntos ganados
    public static event Action OnCorrectAnswer;
    public static event Action OnWrongAnswer;
    public static event Action<int> OnLevelCompleted;    // número del nivel
    public static event Action OnDefeat;
    public static event Action OnDialogueLine;           // cada línea nueva de un diálogo
    public static event Action OnInteract;               // Valentina presiona E frente a algo
    public static event Action OnPanelOpened;            // aparece un panel de la interfaz
    public static event Action OnUIClick;                // clic en un botón
    public static event Action OnHintUsed;               // el jugador pidió una pista

    public static void ItemCollected(int pts) => OnItemCollected?.Invoke(pts);
    public static void CorrectAnswer() => OnCorrectAnswer?.Invoke();
    public static void WrongAnswer() => OnWrongAnswer?.Invoke();
    public static void LevelCompleted(int lvl) => OnLevelCompleted?.Invoke(lvl);
    public static void Defeat() => OnDefeat?.Invoke();
    public static void DialogueLine() => OnDialogueLine?.Invoke();
    public static void Interact() => OnInteract?.Invoke();
    public static void PanelOpened() => OnPanelOpened?.Invoke();
    public static void UIClick() => OnUIClick?.Invoke();
    public static void HintUsed() => OnHintUsed?.Invoke();
}
