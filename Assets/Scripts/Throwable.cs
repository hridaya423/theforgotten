using System.Collections.Generic;
using UnityEngine;

public class Throwable : MonoBehaviour
{
    [Header("Grenade Settings")]
    [SerializeField] float fuseTime = 3f;
    [SerializeField] float damageRadius = 20f;
    [SerializeField] float explosionForce = 1200f;
    [SerializeField] float maxDamage = 100f;

    [Header("Camera Shake")]
    [SerializeField] float maxShakeDistance = 20f;
    [SerializeField] float closeShakeIntensity = 0.2f;
    [SerializeField] float closeShakeDuration = 0.4f;
    [SerializeField] float farShakeIntensity = 0.04f;
    [SerializeField] float farShakeDuration = 0.15f;

    float countdown;
    bool hasExploded = false;
    public bool hasBeenThrown = false;

    public enum ThrowableType
    {
        None,
        Grenade,
        Lethal
    }

    public ThrowableType throwableType = ThrowableType.Grenade;

    private void Start() => countdown = fuseTime;

    private void Update()
    {
        if (hasBeenThrown && !hasExploded)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0f)
            {
                Explode();
                hasExploded = true;
            }
        }
    }

    private void Explode()
    {
        ApplyCameraShake();
        GetThrowableEffect();
        Destroy(gameObject);
    }

    private void ApplyCameraShake()
    {
        if (!CameraShaker.Instance || !GlobalReferences.Instance?.player) return;

        float distance = Vector3.Distance(
            transform.position,
            GlobalReferences.Instance.player.transform.position
        );

        if (distance > maxShakeDistance) return;

        float shakeIntensity = Mathf.Lerp(
            closeShakeIntensity,
            farShakeIntensity,
            distance / maxShakeDistance
        );

        float shakeDuration = Mathf.Lerp(
            closeShakeDuration,
            farShakeDuration,
            distance / maxShakeDistance
        );

        CameraShaker.Instance.TriggerShake(shakeDuration, shakeIntensity);
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
        
        Instantiate(
            GlobalReferences.Instance.grenadeExplosionEffect,
            transform.position,
            Quaternion.identity
        );

        SoundManager.Instance.throwablesChannel.PlayOneShot(
            SoundManager.Instance.grenadeSound
        );

        
        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius);
        HashSet<GameObject> damagedEntities = new HashSet<GameObject>();

        foreach (Collider nearby in colliders)
        {
            ApplyExplosionPhysics(nearby);
            ApplyDamage(nearby, damagedEntities);
        }
    }

    private void ApplyExplosionPhysics(Collider collider)
    {
        Rigidbody rb = collider.GetComponent<Rigidbody>();
        if (rb) rb.AddExplosionForce(explosionForce, transform.position, damageRadius);
    }

    private void ApplyDamage(Collider collider, HashSet<GameObject> damagedEntities)
    {
        GameObject rootObject = collider.transform.root.gameObject;

        
        if (damagedEntities.Contains(rootObject)) return;
        damagedEntities.Add(rootObject);

        float distance = Vector3.Distance(transform.position, rootObject.transform.position);
        int damage = Mathf.RoundToInt(
            Mathf.Clamp(maxDamage * (1 - distance / damageRadius), 0, maxDamage)
        );

        
        Enemy enemy = rootObject.GetComponent<Enemy>();
        if (enemy)
        {
            enemy.TakeDamage(damage);
            return;
        }

        
        Player player = rootObject.GetComponent<Player>();
        if (player)
        {
            player.TakeDamage(damage);
        }
    }

    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}