using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 restPosition;
    private float strength;
    private float duration;
    private float timeLeft;

    void Awake()
    {
        restPosition = transform.localPosition;
    }

    public void Shake(float shakeStrength, float shakeDuration)
    {
        // Don't let a small shake cut off a bigger one that's still going
        if (timeLeft > 0f && shakeStrength < strength) return;

        strength = shakeStrength;
        duration = shakeDuration;
        timeLeft = shakeDuration;
    }

    void LateUpdate()
    {
        if (timeLeft > 0f)
        {
            timeLeft -= Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(timeLeft / duration);
            transform.localPosition = restPosition + (Vector3)(Random.insideUnitCircle * strength * fade);
        }
        else
        {
            strength = 0f;
            transform.localPosition = restPosition;
        }
    }
}
