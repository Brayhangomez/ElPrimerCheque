using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Panel de preguntas de decisión. Se responde con clic o con las teclas 1, 2 y 3.
// Al elegir, la opción se pinta de verde (acierto) o rojo (error) con un ícono, no solo con color.
public class QuestionPanel : MonoBehaviour
{
    public static QuestionPanel Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TMP_Text[] optionTexts;
    [SerializeField] private Image[] optionIcons;
    [SerializeField] private Sprite correctIcon;
    [SerializeField] private Sprite wrongIcon;
    [SerializeField] private Color normalColor = new Color32(242, 230, 208, 255);
    [SerializeField] private Color correctColor = new Color32(150, 200, 120, 255);
    [SerializeField] private Color wrongColor = new Color32(235, 130, 115, 255);

    private DecisionQuestion current;
    private Action<bool> callback;
    private bool locked;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Show(string header, DecisionQuestion q, Action<bool> onAnswer)
    {
        current = q; callback = onAnswer; locked = false;
        headerText.text = header;
        questionText.text = q.question;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool used = i < q.options.Length && !string.IsNullOrEmpty(q.options[i]);
            optionButtons[i].gameObject.SetActive(used);
            if (!used) continue;
            optionTexts[i].text = $"<b>{i + 1}.</b>  {q.options[i]}";
            optionButtons[i].image.color = normalColor;
            optionButtons[i].interactable = true;
            if (optionIcons[i] != null) optionIcons[i].enabled = false;
        }
        panel.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(optionButtons[0].gameObject);
    }

    private void Update()
    {
        if (!IsOpen || locked || PauseMenu.IsPaused) return;
        for (int i = 0; i < optionButtons.Length; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) Choose(i);
    }

    // Lo llaman los botones (On Click) y las teclas 1, 2, 3
    public void Choose(int index)
    {
        if (locked || current == null || index >= current.options.Length) return;
        locked = true;
        GameEvents.UIClick();
        StartCoroutine(Reveal(index, index == current.correctIndex));
    }

    private IEnumerator Reveal(int index, bool correct)
    {
        foreach (var b in optionButtons) b.interactable = false;
        optionButtons[index].image.color = correct ? correctColor : wrongColor;
        if (optionIcons[index] != null) { optionIcons[index].sprite = correct ? correctIcon : wrongIcon; optionIcons[index].enabled = true; }
        yield return new WaitForSecondsRealtime(0.7f);
        panel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        callback?.Invoke(correct);
    }
}
