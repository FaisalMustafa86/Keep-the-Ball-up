using UnityEngine;
using UnityEngine.UI;

// Visual feedback: particle bursts and floating "+1" / "PERFECT" text.
// Objects are created once and reused so nothing is spawned or destroyed during play.
public class Effects : MonoBehaviour
{
    private const int ParticleCount = 80;
    private const int PopupCount = 6;

    private struct Particle
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float life;
        public float maxLife;
        public float size;
    }

    private struct Popup
    {
        public Text text;
        public Vector2 worldPosition;
        public float life;
    }

    private Particle[] particles;
    private Popup[] popups;
    private int nextParticle;
    private int nextPopup;
    private Camera cam;

    public void Init(Camera camera, Canvas canvas, Font font)
    {
        cam = camera;

        Transform parent = new GameObject("Particles").transform;
        particles = new Particle[ParticleCount];
        for (int i = 0; i < ParticleCount; i++)
        {
            GameObject go = new GameObject("Particle");
            go.transform.SetParent(parent);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 20;
            sr.enabled = false;
            particles[i] = new Particle { transform = go.transform, renderer = sr };
        }

        popups = new Popup[PopupCount];
        for (int i = 0; i < PopupCount; i++)
        {
            GameObject go = new GameObject("Popup");
            go.transform.SetParent(canvas.transform, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = 60;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.enabled = false;
            popups[i] = new Popup { text = text };
        }
    }

    public void Burst(Vector2 position, Color color, int count, float power, Vector2 direction)
    {
        for (int n = 0; n < count; n++)
        {
            ref Particle p = ref particles[nextParticle];
            nextParticle = (nextParticle + 1) % ParticleCount;

            Vector2 random = Random.insideUnitCircle.normalized;
            p.velocity = (random + direction).normalized * Random.Range(power * 0.4f, power);
            p.maxLife = Random.Range(0.3f, 0.6f);
            p.life = p.maxLife;
            p.size = Random.Range(0.06f, 0.16f);
            p.transform.position = position;
            p.renderer.color = color;
            p.renderer.enabled = true;
        }
    }

    public void ShowPopup(string message, Vector2 worldPosition, Color color)
    {
        ref Popup popup = ref popups[nextPopup];
        nextPopup = (nextPopup + 1) % PopupCount;

        popup.text.text = message;
        popup.text.color = color;
        popup.text.enabled = true;
        popup.worldPosition = worldPosition;
        popup.life = 1f;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < ParticleCount; i++)
        {
            ref Particle p = ref particles[i];
            if (p.life <= 0f) continue;

            p.life -= dt;
            if (p.life <= 0f)
            {
                p.renderer.enabled = false;
                continue;
            }

            p.velocity *= 1f - 3f * dt;
            p.transform.position += (Vector3)(p.velocity * dt);
            float t = p.life / p.maxLife;
            p.transform.localScale = Vector3.one * p.size * t;
            Color c = p.renderer.color;
            c.a = t;
            p.renderer.color = c;
        }

        for (int i = 0; i < PopupCount; i++)
        {
            ref Popup popup = ref popups[i];
            if (popup.life <= 0f) continue;

            popup.life -= dt * 1.2f;
            if (popup.life <= 0f)
            {
                popup.text.enabled = false;
                continue;
            }

            // Float upwards, pop in at the start and fade out at the end
            popup.worldPosition += Vector2.up * dt * 1.2f;
            popup.text.transform.position = cam.WorldToScreenPoint(popup.worldPosition);
            float age = 1f - popup.life;
            popup.text.transform.localScale = Vector3.one * (age < 0.1f ? Mathf.Lerp(0.5f, 1.2f, age / 0.1f) : Mathf.Lerp(1.2f, 1f, (age - 0.1f) * 3f));
            Color c = popup.text.color;
            c.a = Mathf.Clamp01(popup.life * 2f);
            popup.text.color = c;
        }
    }
}
