using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public enum MusicTrack { Ninguna, Menu, Nivel, RetoFinal, Creditos }

// AudioManager persistente (DontDestroyOnLoad): la música no se corta al cambiar de escena
// y el volumen se controla desde un solo lugar (AudioMixer con grupos Musica y SFX).
// Se crea solo al iniciar el juego a partir del prefab Resources/AudioManager.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes y mezclador")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixer mixer;            // parámetros expuestos "MusicVol" y "SfxVol"

    [Header("Música por escena")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip levelMusic;
    [SerializeField] private AudioClip tenseMusic;         // reto final (nivel 3)
    [SerializeField] private AudioClip creditsMusic;

    [Header("Efectos (SFX)")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip collectClip;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;
    [SerializeField] private AudioClip dialogueClip;
    [SerializeField] private AudioClip panelClip;
    [SerializeField] private AudioClip hintClip;
    [SerializeField] private AudioClip levelClip;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip defeatClip;
    [SerializeField] private AudioClip stepClip;

    private MusicTrack currentTrack = MusicTrack.Ninguna;
    private Coroutine fade;
    private float musicFade = 1f;   // 0..1 para los fundidos entre pistas

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab != null) Instantiate(prefab).name = "AudioManager";
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() => ApplySettings();

    private void OnEnable()
    {
        GameEvents.OnItemCollected += HandleCollected;
        GameEvents.OnCorrectAnswer += HandleCorrect;
        GameEvents.OnWrongAnswer += HandleWrong;
        GameEvents.OnLevelCompleted += HandleLevel;
        GameEvents.OnDefeat += HandleDefeat;
        GameEvents.OnDialogueLine += HandleDialogue;
        GameEvents.OnPanelOpened += HandlePanel;
        GameEvents.OnUIClick += HandleClick;
        GameEvents.OnHintUsed += HandleHint;
        GameSettings.OnChanged += ApplySettings;
    }

    // Se desuscribe al desactivarse para evitar fugas de memoria
    private void OnDisable()
    {
        GameEvents.OnItemCollected -= HandleCollected;
        GameEvents.OnCorrectAnswer -= HandleCorrect;
        GameEvents.OnWrongAnswer -= HandleWrong;
        GameEvents.OnLevelCompleted -= HandleLevel;
        GameEvents.OnDefeat -= HandleDefeat;
        GameEvents.OnDialogueLine -= HandleDialogue;
        GameEvents.OnPanelOpened -= HandlePanel;
        GameEvents.OnUIClick -= HandleClick;
        GameEvents.OnHintUsed -= HandleHint;
        GameSettings.OnChanged -= ApplySettings;
    }

    private void HandleCollected(int _) => PlaySfx(collectClip, 1f, 0.06f);
    private void HandleCorrect() => PlaySfx(correctClip);
    private void HandleWrong() => PlaySfx(wrongClip);
    private void HandleDialogue() => PlaySfx(dialogueClip, 0.7f, 0.08f);
    private void HandlePanel() => PlaySfx(panelClip, 0.6f);
    private void HandleClick() => PlaySfx(clickClip, 0.8f);
    private void HandleHint() => PlaySfx(hintClip);
    private void HandleLevel(int lvl) { StopMusic(0.4f); PlaySfx(lvl >= 3 ? victoryClip : levelClip); }
    private void HandleDefeat() { StopMusic(0.4f); PlaySfx(defeatClip); }

    // ---------------------------------------------------------------- música
    public void PlayMusic(MusicTrack track)
    {
        if (track == currentTrack && musicSource.isPlaying) return;
        currentTrack = track;
        AudioClip clip = track switch
        {
            MusicTrack.Menu => menuMusic,
            MusicTrack.Nivel => levelMusic,
            MusicTrack.RetoFinal => tenseMusic,
            MusicTrack.Creditos => creditsMusic,
            _ => null
        };
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(CrossFade(clip));
    }

    public void StopMusic(float seconds = 0.5f)
    {
        currentTrack = MusicTrack.Ninguna;
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(CrossFade(null, seconds));
    }

    private IEnumerator CrossFade(AudioClip next, float seconds = 0.6f)
    {
        // baja la pista actual (tiempo sin escala: funciona aunque el juego esté en pausa)
        for (float t = 0; t < seconds && musicSource.isPlaying; t += Time.unscaledDeltaTime)
        {
            musicFade = 1f - t / seconds; ApplyMusicFade(); yield return null;
        }
        musicSource.Stop();
        if (next == null) yield break;
        musicSource.clip = next; musicSource.loop = true; musicSource.Play();
        for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
        {
            musicFade = t / seconds; ApplyMusicFade(); yield return null;
        }
        musicFade = 1f; ApplyMusicFade();
    }

    private void ApplyMusicFade() => musicSource.volume = musicFade * (mixer == null ? EffectiveMusic() : 1f);

    // ---------------------------------------------------------------- efectos
    public void PlaySfx(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        sfxSource.PlayOneShot(clip, volume * (mixer == null ? EffectiveSfx() : 1f));
    }

    public void PlayStep() => PlaySfx(stepClip, 0.35f, 0.12f);

    // ---------------------------------------------------------------- volumen
    private float EffectiveMusic() => GameSettings.Muted ? 0f : GameSettings.MusicVolume;
    private float EffectiveSfx() => GameSettings.Muted ? 0f : GameSettings.SfxVolume;

    // Slider 0..1 -> decibeles del AudioMixer
    private static float ToDb(float v) => Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;

    public void ApplySettings()
    {
        if (mixer != null)
        {
            mixer.SetFloat("MusicVol", ToDb(EffectiveMusic()));
            mixer.SetFloat("SfxVol", ToDb(EffectiveSfx()));
        }
        ApplyMusicFade();
    }
}
