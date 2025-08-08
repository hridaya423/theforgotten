using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{

    public int bulletDamage;


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            print("hit" + collision.gameObject.name);
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
            if (collision.gameObject.GetComponent<Enemy>().isDead)
            {
                return;
            }
            collision.gameObject.GetComponent<Enemy>().TakeDamage(bulletDamage);
            CreateBloodSprayEffect(collision);
            Destroy(gameObject);
        }
    }

    private void CreateBloodSprayEffect(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        GameObject bloodspray = Instantiate(
            GlobalReferences.Instance.bloodSprayEffect,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );
        bloodspray.transform.SetParent(collision.gameObject.transform);
    }

    void CreateBulletImpactEffect(Collision collision)
    {

        ContactPoint contact = collision.contacts[0];
        GameObject hole = Instantiate(
            GlobalReferences.Instance.bulletImpactEffectprefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );
        hole.transform.SetParent(collision.gameObject.transform);
    }
}

