using UnityEngine;
using static Weapon;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;

    public float shakeSpeed = 50f
    public float returnSpeed = 5f; 

    private Vector3 originalPos;
    private Quaternion originalRot;
    private float currentShakeDuration = 0f;
    private float currentShakeMagnitude = 0f;

    void Awake()
    {
        Instance = this;
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
    }

    void LateUpdate()
    {
        if (currentShakeDuration > 0)
        {
            float x = Mathf.Sin(Time.time * shakeSpeed) * currentShakeMagnitude;
            float y = Mathf.Cos(Time.time * shakeSpeed * 0.9f) * currentShakeMagnitude;
            transform.localPosition = originalPos + new Vector3(x, y, 0);
            float rotation = Mathf.Sin(Time.time * shakeSpeed * 0.7f) * currentShakeMagnitude * 20f;
            transform.localRotation = originalRot * Quaternion.Euler(0, 0, rotation);
            currentShakeDuration -= Time.deltaTime;

            if (currentShakeDuration <= 0)
            {
                currentShakeMagnitude = 0f;
            }
        }
        else
        {            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos, Time.deltaTime * returnSpeed);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRot, Time.deltaTime * returnSpeed);
        }
    }

    public void TriggerShake(float duration = 0.15f, float magnitude = 0.05f)
    {
        currentShakeDuration = duration;
        currentShakeMagnitude = magnitude;
    }

    public void ShakeLight()
    {
        TriggerShake(0.1f, 0.02f);
    }

    public void ShakeMedium()
    {
        TriggerShake(0.15f, 0.04f);
    }

    public void ShakeHeavy()
    {
        TriggerShake(0.2f, 0.06f);
    }

    public void ShakeExtreme()
    {
        TriggerShake(0.3f, 0.08f);
    }
    public void AddTrauma(float amount)
    {
        TriggerShake(0.15f, amount * 0.1f);
    }
}