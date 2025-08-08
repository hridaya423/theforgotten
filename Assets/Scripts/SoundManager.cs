using UnityEngine;
using static Weapon;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set; }

    public AudioSource ShootingChannel;
    public AudioSource emptymag;
    public AudioSource reloadpistol;
    public AudioClip m1911shoot;
    public AudioClip ak74shoot;
    public AudioClip uziShoot;
    public AudioSource reloaduzi;
    public AudioSource reloadak;

    public AudioSource throwablesChannel;

    public AudioClip grenadeSound;
    public AudioClip smokeSound;

    public AudioClip zombieWalking;
    public AudioClip zombieChasing;
    public AudioClip zombieAttacking;
    public AudioClip zombieDeath;
    public AudioClip zombieHurt;

    public AudioSource zombieChannel;
    public AudioSource zombieChannel1;

    public AudioSource playerChannel;
    public AudioClip playerHurt;
    public AudioClip playerDeath;

    public AudioClip gameOversfx;

    [Header("Boss Sound Effects")]
    public AudioClip bossSpawnSound;
    public AudioClip bossAttackSound;
    public AudioClip bossSpecialAttackSound;
    public AudioClip bossDeathSound;
    public AudioClip bossEnrageSound;

    
    [Header("Boss Audio Channel")]
    public AudioSource bossChannel;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayShootingSound(WeaponModel weapon)
    {
        switch(weapon)
        {
            case WeaponModel.M1911:
                ShootingChannel.PlayOneShot(m1911shoot);
                break;
            case WeaponModel.AK74:
                ShootingChannel.PlayOneShot(ak74shoot);
                break;
            case WeaponModel.Uzi:
                ShootingChannel.PlayOneShot(uziShoot);
                break;
        }
    }

    public void PlayReloadSound(WeaponModel weapon)
    {
        switch (weapon)
        {
            case WeaponModel.M1911:
                reloadpistol.Play();
                break;
            case WeaponModel.AK74:
                reloadak.Play();
                break;
            case WeaponModel.Uzi:
                reloaduzi.Play();
                break;
        }
    }

}

