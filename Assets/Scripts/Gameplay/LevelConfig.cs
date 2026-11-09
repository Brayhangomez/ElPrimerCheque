using UnityEngine;

// Valores de dificultad de cada nivel guardados en un ScriptableObject (Actividad 2.1).
// Así se ajusta la dificultad desde el Inspector sin tocar código.
[CreateAssetMenu(fileName = "Nivel_Config", menuName = "El Primer Cheque/Configuración de nivel")]
public class LevelConfig : ScriptableObject
{
    [Header("Identificación")]
    public int levelNumber = 1;
    public string chapterTitle = "Capítulo 1: El primer sueldo";
    [Tooltip("Rol pedagógico del nivel")]
    [TextArea(2, 4)] public string pedagogicalRole = "Tutorial guiado";
    [Tooltip("Escena que se carga al ganar. Vacío = último nivel (va a los créditos)")]
    public string nextScene = "Nivel2";
    public MusicTrack music = MusicTrack.Nivel;

    [Header("Objetivos")]
    public int coinsRequired = 5;
    [Tooltip("Si está activo, al cumplir los objetivos hay que volver a hablar con el NPC")]
    public bool requireFinalTalk = true;

    [Header("Dificultad")]
    [Tooltip("Segundos para terminar el nivel. 0 = sin límite de tiempo")]
    public float timeLimit = 0f;
    [Tooltip("Vidas (intentos). 0 = intentos ilimitados")]
    public int lives = 0;
    [Tooltip("Pistas disponibles. -1 = ilimitadas")]
    public int hintsAvailable = -1;
    [Tooltip("Puntos que cuesta cada pista")]
    public int hintCost = 0;
    [Tooltip("Puntos que se pierden al responder mal")]
    public int wrongAnswerPenalty = 0;
    [Tooltip("Mostrar el dato educativo antes de la pregunta")]
    public bool showInfoBeforeQuestion = true;
    [Tooltip("Mostrar mensajes de tutorial (solo nivel 1)")]
    public bool showTutorial = false;

    [Header("Puntaje")]
    public int pointsPerCoin = 100;
    public int pointsCorrectFirstTry = 200;
    public int pointsCorrectRetry = 100;
    public int pointsPerSecondLeft = 2;
}
