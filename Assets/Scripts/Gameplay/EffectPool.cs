using System.Collections.Generic;
using UnityEngine;

// Pool de efectos de partículas: en vez de crear y destruir un efecto cada vez que se recoge
// una moneda, se reutilizan unos pocos (object pooling, recomendado en la guía para el rendimiento).
public class EffectPool : MonoBehaviour
{
    public static EffectPool Instance { get; private set; }

    [SerializeField] private ParticleSystem coinBurstPrefab;
    [SerializeField] private ParticleSystem correctBurstPrefab;
    [SerializeField] private ParticleSystem confettiPrefab;
    [SerializeField] private int sizePerEffect = 4;

    private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>> pools = new Dictionary<ParticleSystem, Queue<ParticleSystem>>();

    private void Awake()
    {
        Instance = this;
        foreach (var p in new[] { coinBurstPrefab, correctBurstPrefab, confettiPrefab })
        {
            if (p == null) continue;
            var q = new Queue<ParticleSystem>();
            for (int i = 0; i < sizePerEffect; i++)
            {
                var ps = Instantiate(p, transform);
                ps.gameObject.SetActive(false);
                q.Enqueue(ps);
            }
            pools[p] = q;
        }
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void CoinBurst(Vector3 pos) => Play(coinBurstPrefab, pos);
    public void CorrectBurst(Vector3 pos) => Play(correctBurstPrefab, pos);
    public void Confetti(Vector3 pos) => Play(confettiPrefab, pos);

    private void Play(ParticleSystem prefab, Vector3 pos)
    {
        if (prefab == null || !pools.TryGetValue(prefab, out var q)) return;
        var ps = q.Dequeue();          // toma el más antiguo y lo vuelve a poner al final
        q.Enqueue(ps);
        ps.transform.position = pos;
        ps.gameObject.SetActive(true);
        ps.Clear(true);
        ps.Play(true);
    }
}
