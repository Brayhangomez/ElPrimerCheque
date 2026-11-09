using System;
using UnityEngine;

// Opciones de accesibilidad y audio. Se guardan con PlayerPrefs para que se mantengan al cerrar el juego.
public static class GameSettings
{
    public static event Action OnChanged;

    public static float MusicVolume { get; private set; }
    public static float SfxVolume { get; private set; }
    public static bool Muted { get; private set; }
    public static bool LargeText { get; private set; }
    public static bool HighContrast { get; private set; }
    public static bool CameraShake { get; private set; }

    static GameSettings() { Load(); }

    public static void Load()
    {
        MusicVolume = PlayerPrefs.GetFloat("opt_musica", 0.7f);
        SfxVolume = PlayerPrefs.GetFloat("opt_sfx", 0.85f);
        Muted = PlayerPrefs.GetInt("opt_silencio", 0) == 1;
        LargeText = PlayerPrefs.GetInt("opt_texto_grande", 0) == 1;
        HighContrast = PlayerPrefs.GetInt("opt_contraste", 0) == 1;
        CameraShake = PlayerPrefs.GetInt("opt_sacudida", 1) == 1;
    }

    public static void SetMusicVolume(float v) { MusicVolume = Mathf.Clamp01(v); Save(); }
    public static void SetSfxVolume(float v) { SfxVolume = Mathf.Clamp01(v); Save(); }
    public static void SetMuted(bool b) { Muted = b; Save(); }
    public static void SetLargeText(bool b) { LargeText = b; Save(); }
    public static void SetHighContrast(bool b) { HighContrast = b; Save(); }
    public static void SetCameraShake(bool b) { CameraShake = b; Save(); }

    static void Save()
    {
        PlayerPrefs.SetFloat("opt_musica", MusicVolume);
        PlayerPrefs.SetFloat("opt_sfx", SfxVolume);
        PlayerPrefs.SetInt("opt_silencio", Muted ? 1 : 0);
        PlayerPrefs.SetInt("opt_texto_grande", LargeText ? 1 : 0);
        PlayerPrefs.SetInt("opt_contraste", HighContrast ? 1 : 0);
        PlayerPrefs.SetInt("opt_sacudida", CameraShake ? 1 : 0);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
