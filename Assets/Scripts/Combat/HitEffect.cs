using UnityEngine;

/// <summary>
/// Visual hit effect that appears when a bullet hits a player.
/// Mermi oyuncuya isabet ettiğinde görünen görsel efekt.
/// Tüm client'larda gösterilir, kısa sürede kaybolur.
/// </summary>
public class HitEffect : MonoBehaviour
{
    [SerializeField] private float _duration = 0.3f;
    [SerializeField] private float _maxScale = 1.5f;

    private float _timer;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
        }
        _sr.sortingOrder = 20; // Her şeyin üstünde
    }

    public void Initialize(Color color)
    {
        if (_sr != null)
        {
            _sr.color = color;
        }
        _timer = _duration;
        transform.localScale = Vector3.one * 0.3f;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Büyüyüp solan animasyon
        float progress = 1f - (_timer / _duration);
        float scale = Mathf.Lerp(0.3f, _maxScale, progress);
        transform.localScale = Vector3.one * scale;

        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = Mathf.Lerp(1f, 0f, progress); // Şeffaflaş
            _sr.color = c;
        }
    }
}
