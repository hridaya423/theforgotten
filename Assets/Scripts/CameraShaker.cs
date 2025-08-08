using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;

    private Vector3 originalPos;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private float dampingSpeed = 1.0f;
    private float initialDuration = 0f;

    
    private Vector3 currentShakeOffset = Vector3.zero;
    private Vector3 shakeVelocity = Vector3.zero;

    void Awake()
    {
        Instance = this;
        originalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (shakeDuration > 0)
        {
            
            float x = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f;

            Vector3 targetShake = new Vector3(x, y, 0f) * shakeMagnitude;

            
            currentShakeOffset = Vector3.SmoothDamp(
                currentShakeOffset,
                targetShake,
                ref shakeVelocity,
                0.1f  
            );

            transform.localPosition = originalPos + currentShakeOffset;

            
            float falloff = 1f - (shakeDuration / initialDuration);
            falloff = Mathf.Clamp01(falloff);

            shakeDuration -= Time.deltaTime * dampingSpeed;
            shakeMagnitude = Mathf.Lerp(shakeMagnitude, 0f, Time.deltaTime * 3f);
        }
        else
        {
            shakeDuration = 0f;

            
            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition,
                originalPos,
                ref shakeVelocity,
                0.1f
            );

            currentShakeOffset = Vector3.Lerp(currentShakeOffset, Vector3.zero, Time.deltaTime * 5f);
        }
    }

    public void TriggerShake(float duration = 0.15f, float magnitude = 0.4f)
    {
        
        if (shakeDuration > 0)
        {
            
            shakeMagnitude = Mathf.Min(shakeMagnitude + magnitude * 0.5f, magnitude * 1.2f);
            shakeDuration = Mathf.Max(shakeDuration, duration);
        }
        else
        {
            shakeMagnitude = magnitude;
            shakeDuration = duration;
        }

        initialDuration = duration;
        dampingSpeed = 1.0f;
    }
}