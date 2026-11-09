using UnityEngine;
using TMPro;

// Sistema de pistas con límite por nivel (Actividad 2.3). Se pide con la tecla H o con el botón del HUD.
// La pista depende de lo que le falte al jugador: hablar con Don Rafael, una moneda o una decisión.
public class HintSystem : MonoBehaviour
{
    [SerializeField] private TMP_Text hintCounterText;     // "Pistas: 2" en el HUD
    [SerializeField] private Transform coinsRoot;           // contenedor de las monedas

    private void Start()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.OnStateChanged -= Refresh;
    }

    private void Update()
    {
        if (PauseMenu.IsPaused || GameInput.MovementLocked) return;
        if (Input.GetKeyDown(KeyCode.H)) RequestHint();
    }

    private void Refresh()
    {
        var lm = LevelManager.Instance;
        if (lm == null || hintCounterText == null) return;
        int left = lm.HintsLeft;
        hintCounterText.text = left == int.MaxValue ? "Pista (H)" : $"Pistas: {left} (H)";
    }

    // Lo llama la tecla H y el botón "Pista" del HUD
    public void RequestHint()
    {
        var lm = LevelManager.Instance;
        if (lm == null || lm.Ended) return;
        if (lm.HintsLeft <= 0)
        {
            FeedbackUI.Instance?.Show(FeedbackType.Info, "Ya no te quedan pistas", "En este nivel las pistas son limitadas. ¡Tú puedes!", 3f);
            return;
        }
        string hint = BuildHint(lm);
        lm.TryUseHint();
        string cost = lm.Config.hintCost > 0 ? $"  (-{lm.Config.hintCost} pts)" : "";
        FeedbackUI.Instance?.Show(FeedbackType.Pista, "Pista" + cost, hint, 5f);
    }

    private string BuildHint(LevelManager lm)
    {
        Vector3 p = lm.Player != null ? lm.Player.position : Vector3.zero;
        // solo se pide hablar primero si todavía no hay ningún avance
        if (!lm.TalkedToNpc && lm.Npc != null && lm.Coins == 0 && lm.DecisionsSolved == 0)
            return $"Empieza hablando con Don Rafael: está {Donde(p, lm.Npc.position)}, frente a los Apartamentos. Acércate y presiona E.";
        if (lm.MissionReady)
            return $"¡Ya tienes todo! Vuelve con Don Rafael ({Donde(p, lm.Npc.position)}) y presiona E.";

        // lo más cercano entre una moneda y una decisión pendiente
        Transform coin = NearestCoin(p);
        var decision = lm.NextPendingDecision(p);
        bool faltanMonedas = lm.Coins < lm.CoinsRequired && coin != null;
        if (decision != null && (!faltanMonedas || Mathf.Abs(decision.transform.position.x - p.x) < Mathf.Abs(coin.position.x - p.x)))
            return $"Te falta tomar una decisión en {decision.PlaceName}: está {Donde(p, decision.transform.position)}. Lee bien y piensa en la regla 50/30/20.";
        if (faltanMonedas)
            return $"La moneda de ahorro más cercana está {Donde(p, coin.position)}. Te faltan {lm.CoinsRequired - lm.Coins}.";
        return "Revisa el objetivo en la esquina superior izquierda.";
    }

    private Transform NearestCoin(Vector3 p)
    {
        if (coinsRoot == null) return null;
        Transform best = null; float bd = float.MaxValue;
        foreach (Transform c in coinsRoot)
        {
            if (!c.gameObject.activeInHierarchy) continue;
            float d = Mathf.Abs(c.position.x - p.x);
            if (d < bd) { bd = d; best = c; }
        }
        return best;
    }

    private static string Donde(Vector3 from, Vector3 to)
    {
        float dx = to.x - from.x;
        int m = Mathf.RoundToInt(Mathf.Abs(dx));
        if (m <= 1) return "aquí al lado";
        return $"a {m} pasos hacia la {(dx > 0 ? "derecha" : "izquierda")}";
    }
}
