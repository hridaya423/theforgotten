using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public int weaponDamage;
    public bool isActiveWeapon;
    public bool isShooting, readyToShoot;
    bool allowReset = true;
    public float shootingDelay = 0.2f;
    public int bulletsPerBurst = 3;
    public int currentBurst;
    public float spreadIntensity;
    public float hipSpreadIntensity;
    public float ADSSpreadIntensity;

    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletSpeed = 300;
    public float bulletPrefabLifetime = 3f;

    public GameObject muzzleEffect;
    internal Animator animator;

    public float reloadTime;
    public int magazineSize, bulletsLeft;
    public bool isReloading;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    bool isADS;

    public enum WeaponModel
    {
        M1911,
        AK74,
        Shotgun,
        Uzi
    }

    public WeaponModel thisWeaponModel;

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode currentshootingMode;

    private void Awake()
    {
        readyToShoot = true;
        currentBurst = bulletsPerBurst;
        animator = GetComponent<Animator>();
        bulletsLeft = magazineSize;
        spreadIntensity = hipSpreadIntensity;
    }

    void Update()
    {
        if (isActiveWeapon)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("WeaponRender");
            }

            if (Input.GetMouseButtonDown(1))
            {
                EnterADS();
            }

            if (Input.GetMouseButtonUp(1))
            {
                ExitADS();
            }

            GetComponent<Outline>().enabled = false;

            if (bulletsLeft == 0 && isShooting)
            {
                SoundManager.Instance.emptymag.Play();
            }
            if (currentshootingMode == ShootingMode.Auto)
            {
                isShooting = Input.GetKey(KeyCode.Mouse0);
            }
            else if (currentshootingMode == ShootingMode.Burst || currentshootingMode == ShootingMode.Single)
            {
                isShooting = Input.GetKeyDown(KeyCode.Mouse0);
            }

            if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !isReloading && WeaponManager.Instance.CheckAmmoLeftFor(thisWeaponModel) > 0)
            {
                Reload();
            }

            if (readyToShoot && isShooting && bulletsLeft > 0)
            {
                currentBurst = bulletsPerBurst;
                FireWeapon();
            }
            else
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
                }
            }
        }
    }

    private void EnterADS()
    {
        animator.SetTrigger("enterADS");
        isADS = true;
        HUDManager.Instance.middleDot.SetActive(false);
        spreadIntensity = ADSSpreadIntensity;
    }

    private void ExitADS()
    {
        animator.SetTrigger("exitADS");
        isADS = false;
        HUDManager.Instance.middleDot.SetActive(true);
        spreadIntensity = hipSpreadIntensity;
    }

    private void FireWeapon()
    {
        bulletsLeft--;

        muzzleEffect.GetComponent<ParticleSystem>().Play();
        if (isADS)
        {
            animator.SetTrigger("ADS_RECOIL");
            isADS = true;
        }
        else
        {
            animator.SetTrigger("RECOIL");
            isADS = false;
        }

        SoundManager.Instance.PlayShootingSound(thisWeaponModel);

        
        AddCameraShakeForWeapon();

        
        PerformHitScan();

        readyToShoot = false;

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);

        Bullet bul = bullet.GetComponent<Bullet>();
        bul.bulletDamage = weaponDamage;

        bullet.transform.forward = shootingDirection;
        bullet.GetComponent<Rigidbody>().AddForce(shootingDirection * bulletSpeed, ForceMode.Impulse);

        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifetime));

        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = false;
        }

        if (currentshootingMode == ShootingMode.Burst)
        {
            currentBurst--;
            Invoke("FireWeapon", shootingDelay);
        }
    }

    
    private void AddCameraShakeForWeapon()
    {
        if (CameraShaker.Instance == null) return;

        
        switch (thisWeaponModel)
        {
            case WeaponModel.M1911:
                CameraShaker.Instance.TriggerShake(0.06f, 0.04f);  
                break;
            case WeaponModel.AK74:
                CameraShaker.Instance.TriggerShake(0.08f, 0.06f);  
                break;
            case WeaponModel.Shotgun:
                CameraShaker.Instance.TriggerShake(0.12f, 0.12f);  
                break;
            case WeaponModel.Uzi:
                CameraShaker.Instance.TriggerShake(0.04f, 0.03f);  
                break;
            default:
                CameraShaker.Instance.TriggerShake(0.06f, 0.04f);
                break;
        }
    }

    
    private void PerformHitScan()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            
            ZombieSpawnController spawner = hit.collider.GetComponent<ZombieSpawnController>();
            if (spawner != null)
            {
                spawner.TakeDamage(weaponDamage);
                

                
                if (CameraShaker.Instance != null)
                {
                    CameraShaker.Instance.TriggerShake(0.1f, 0.08f);
                }
            }
        }
    }

    private void Reload()
    {
        animator.SetTrigger("RELOAD");
        SoundManager.Instance.PlayReloadSound(thisWeaponModel);
        isReloading = true;
        Invoke("ReloadFinished", reloadTime);
    }

    private void ReloadFinished()
    {
        int bulletsNeeded = magazineSize - bulletsLeft;
        int availableAmmo = WeaponManager.Instance.CheckAmmoLeftFor(thisWeaponModel);
        int bulletsToReload = Math.Min(bulletsNeeded, availableAmmo);

        bulletsLeft += bulletsToReload;

        WeaponManager.Instance.DecreaseTotalAmmo(bulletsToReload, thisWeaponModel);

        isReloading = false;
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }

    private Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000);
        }

        Vector3 direction = targetPoint - bulletSpawn.position;

        float x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);
        float y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);

        return direction + new Vector3(x, y, 0);
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }
}