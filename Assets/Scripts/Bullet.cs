using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int bulletDamage;

    [Header("Cleanup")]
    [SerializeField] private float bulletLifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, bulletLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            print("hit " + collision.gameObject.name);
            CreateBulletImpactEffect(collision);
            Destroy(gameObject);
        }
        if (collision.gameObject.CompareTag("Wall"))
        {
            print("hit a wall");
            CreateBulletImpactEffect(collision);
            Destroy(gameObject);
        }
        if (collision.gameObject.CompareTag("Zombie"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null && enemy.isDead)
            {
                return;
            }
            CreateBloodSprayEffect(collision);
            Destroy(gameObject);
        }
    }

    private void CreateBloodSprayEffect(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        if (GlobalReferences.Instance.bloodSprayEffect != null)
        {
            GameObject bloodspray = Instantiate(
                GlobalReferences.Instance.bloodSprayEffect,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );
            Destroy(bloodspray, 5f);
        }
    }

    void CreateBulletImpactEffect(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        if (GlobalReferences.Instance.bulletImpactEffectprefab != null)
        {
            GameObject hole = Instantiate(
                GlobalReferences.Instance.bulletImpactEffectprefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );
            hole.transform.SetParent(collision.gameObject.transform);
            Destroy(hole, 30f);
        }
    }
}