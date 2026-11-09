using UnityEngine;
using TMPro;

// Mensajes contextuales del tutorial (solo nivel 1). Cada mensaje desaparece cuando el jugador
// ya hizo lo que se le pedía (Actividad 2.3: "Pulsa E para hablar" desaparece al hablar).
public class TutorialHints : MonoBehaviour
{
    [SerializeField] private CanvasGroup bubble;
    [SerializeField] private TMP_Text bubbleText;

    private int step;
    private float stepTime;
    private float startX;
    private bool hintUsed;

    private static readonly string[] Steps =
    {
        "Usa <b>A / D</b> o las <b>flechas</b> para caminar  ·  <b>Shift</b> para correr",
        "Acércate a Don Rafael y presiona <b>E</b> para hablar",
        "Pasa por encima de las <b>monedas</b> para guardarlas en tu ahorro",
        "¿Perdido? Presiona <b>H</b> y te damos una pista",
        "En la <b>vitrina</b> y en el <b>banco</b> presiona <b>E</b> y responde con <b>1, 2 o 3</b>",
        ""
    };

    private void Start()
    {
        var lm = LevelManager.Instance;
        if (lm == null || !lm.Config.showTutorial) { gameObject.SetActive(false); return; }
        if (lm.Player != null) startX = lm.Player.position.x;
        GameEvents.OnHintUsed += MarkHint;
        Show(0);
    }

    private void OnDestroy() { GameEvents.OnHintUsed -= MarkHint; }
    private void MarkHint() { hintUsed = true; }

    private void Show(int s)
    {
        step = s; stepTime = 0f;
        bubbleText.text = Steps[Mathf.Min(s, Steps.Length - 1)];
    }

    private void Update()
    {
        var lm = LevelManager.Instance;
        if (lm == null) return;
        stepTime += Time.unscaledDeltaTime;

        // se esconde mientras hay un diálogo, un cartel o una pregunta abierta
        bool visible = step < Steps.Length - 1 && !GameInput.MovementLocked && !PauseMenu.IsPaused && !lm.Ended;
        bubble.alpha = Mathf.MoveTowards(bubble.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 4f);

        bool done = step switch
        {
            0 => lm.Player != null && Mathf.Abs(lm.Player.position.x - startX) > 1.5f,
            1 => lm.TalkedToNpc,
            2 => lm.Coins > 0,
            3 => hintUsed || stepTime > 12f,
            4 => lm.DecisionsSolved > 0,
            _ => false
        };
        if (done && step < Steps.Length - 1) Show(step + 1);
    }
}
