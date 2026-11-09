using UnityEngine;

// Indica qué pista de música suena en la escena (menú, nivel, reto final o créditos).
public class SceneMusic : MonoBehaviour
{
    [SerializeField] private MusicTrack track = MusicTrack.Menu;

    private void Start()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(track);
    }
}
