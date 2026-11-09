using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Panel de Opciones (accesibilidad y audio): volumen de música y efectos, silenciar,
// texto grande, alto contraste y vibración de cámara. Los cambios se aplican al instante.
public class OptionsPanel : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Toggle largeTextToggle;
    [SerializeField] private Toggle contrastToggle;
    [SerializeField] private Toggle shakeToggle;

    private bool loading;

    private void Awake()
    {
        musicSlider.onValueChanged.AddListener(v => { if (!loading) GameSettings.SetMusicVolume(v); });
        sfxSlider.onValueChanged.AddListener(v => { if (!loading) GameSettings.SetSfxVolume(v); });
        muteToggle.onValueChanged.AddListener(b => { if (!loading) { GameSettings.SetMuted(b); GameEvents.UIClick(); } });
        largeTextToggle.onValueChanged.AddListener(b => { if (!loading) { GameSettings.SetLargeText(b); GameEvents.UIClick(); } });
        contrastToggle.onValueChanged.AddListener(b => { if (!loading) { GameSettings.SetHighContrast(b); GameEvents.UIClick(); } });
        shakeToggle.onValueChanged.AddListener(b => { if (!loading) { GameSettings.SetCameraShake(b); GameEvents.UIClick(); } });
    }

    private void OnEnable()
    {
        loading = true;
        musicSlider.value = GameSettings.MusicVolume;
        sfxSlider.value = GameSettings.SfxVolume;
        muteToggle.isOn = GameSettings.Muted;
        largeTextToggle.isOn = GameSettings.LargeText;
        contrastToggle.isOn = GameSettings.HighContrast;
        shakeToggle.isOn = GameSettings.CameraShake;
        loading = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
    }

    // Efecto de prueba al soltar el slider de efectos
    public void PreviewSfx() => GameEvents.UIClick();
}
