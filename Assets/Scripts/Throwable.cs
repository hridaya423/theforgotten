using System;
using UnityEngine;

public class Throwable : MonoBehaviour
{
    [SerializeField] float delay = 3f;
    [SerializeField] float damageRadius = 20f;
    [SerializeField] float explosionForce = 1200f;
    [SerializeField] float maxDamage = 100f;

    float countdown;

    bool hasExploded = false;
    public bool hasBeenThrown = false;

    public enum ThrowableType
    {
        None,   
        Grenade
    }

    public ThrowableType throwableType;

    private void Start()
    {
        countdown = delay;
    }

    private void Update()
    {
        if (hasBeenThrown)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0f && !hasExploded)
            {
                Explode();
                hasExploded = true;
            }
        }
    }

    private void Explode()
    {
        GetThrowableEffect();
        if (CameraShaker.Instance != null && GlobalReferences.Instance?.player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, GlobalReferences.Instance.player.transform.position);

            if (distanceToPlayer < 20f)
            {
                
                float shakeIntensity = Mathf.Lerp(0.2f, 0.04f, distanceToPlayer / 20f);
                float shakeDuration = Mathf.Lerp(0.4f, 0.15f, distanceToPlayer / 20f);

                CameraShaker.Instance.TriggerShake(shakeDuration, shakeIntensity);
            }
        }
        Destroy(gameObject);

    }

    private void GetThrowableEffect()
    {
        switch (throwableType)
        {
            case ThrowableType.Grenade:
                GrenadeEffect();
                break;
            
        }
    }

    private void GrenadeEffect()
    {
        GameObject explosionEffect = GlobalReferences.Instance.grenadeExplosionEffect;
        Instantiate(explosionEffect, transform.position, transform.rotation);

        SoundManager.Instance.throwablesChannel.PlayOneShot(SoundManager.Instance.grenadeSound);

        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (Collider objectInRange in colliders)
        {
            Rigidbody rb = objectInRange.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, damageRadius);
            }

            if (objectInRange.gameObject.GetComponent<Enemy>())
            {
                Enemy zombie = objectInRange.gameObject.GetComponent<Enemy>();
                float distance = Vector3.Distance(transform.position, zombie.transform.position);
                int damage = Mathf.RoundToInt(Mathf.Clamp(maxDamage * (1 - (distance / damageRadius)), 0, maxDamage));
                zombie.TakeDamage(damage);
               
            }
        }
    }
}

