using UnityEngine;
using UnityEngine.UI;

// Hace sonar el clic de interfaz en cualquier botón (lanza el evento; el AudioManager lo reproduce).
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    private void Awake() { GetComponent<Button>().onClick.AddListener(GameEvents.UIClick); }
}
