using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class HUDManager : MonoBehaviour
{

    [Header("Ammo")]
    public TextMeshProUGUI magazineAmmoUI;
    public TextMeshProUGUI totalAmmoUI;
    public Image ammoTypeUI;

    [Header("Weapon")]
    public Image activeWeaponUI;
    public Image unActiveWeaponUI;

    [Header("Throwables")]
    public Image lethalUI;
    public TextMeshProUGUI lethalAmountUI;
    
    public Image tacticalUI;
    public TextMeshProUGUI tacticalAmountUI;

    public Sprite emptySlot;
    public Sprite greySlot;

    public GameObject middleDot;
    
    public static HUDManager Instance { get; set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        Weapon activeWeapon = WeaponManager.Instance.activeWeaponSlot.GetComponentInChildren<Weapon>();
        Weapon unActiveWeapon = GetUnActiveWeaponSlot().GetComponentInChildren<Weapon>();

        if (activeWeapon)
        {
            magazineAmmoUI.text = $"{activeWeapon.bulletsLeft/activeWeapon.bulletsPerBurst}";
            totalAmmoUI.text = WeaponManager.Instance.CheckAmmoLeftFor(activeWeapon.thisWeaponModel).ToString();

            Weapon.WeaponModel model = activeWeapon.thisWeaponModel;
            ammoTypeUI.sprite = GetAmmoSprite(model);

            activeWeaponUI.sprite = GetWeaponSprite(model);

            if (unActiveWeapon)
            {
                unActiveWeaponUI.sprite = GetWeaponSprite(unActiveWeapon.thisWeaponModel);
            }
        }
        else
        {
            magazineAmmoUI.text = "";
            totalAmmoUI.text = "";
            
            ammoTypeUI.sprite = emptySlot;
            activeWeaponUI.sprite = emptySlot;
            unActiveWeaponUI.sprite = emptySlot;
        }

        if (WeaponManager.Instance.lethalsCount <= 0)
        {
            lethalUI.sprite = greySlot;
        }

        if (WeaponManager.Instance.tacticalsCount <= 0)
        {
            tacticalUI.sprite = greySlot;
        }
    }

    private Sprite GetWeaponSprite(Weapon.WeaponModel model)
    {
        switch (model)
        {
            case Weapon.WeaponModel.M1911:
                return Resources.Load<GameObject>("M1191_Weapon").GetComponent<SpriteRenderer>().sprite;
            case Weapon.WeaponModel.AK74:
                return Resources.Load<GameObject>("AK74_Weapon").GetComponent<SpriteRenderer>().sprite;
            case Weapon.WeaponModel.Uzi:
                return Resources.Load<GameObject>("Uzi_Weapon").GetComponent<SpriteRenderer>().sprite;
            case Weapon.WeaponModel.Shotgun:
                return null;
            default:
                return null;
        }
    }

    private Sprite GetAmmoSprite(Weapon.WeaponModel model)
    {
        switch (model)
        {
            case Weapon.WeaponModel.M1911:
                return Resources.Load<GameObject>("PistolAmmo").GetComponent<SpriteRenderer>().sprite;
            case Weapon.WeaponModel.AK74:
                return Resources.Load<GameObject>("RifleAmmo").GetComponent<SpriteRenderer>().sprite;
            case Weapon.WeaponModel.Uzi:
                return Resources.Load<GameObject>("SMGAmmo").GetComponent<SpriteRenderer>().sprite;
            case Weapon.WeaponModel.Shotgun:
                return null;
            default:
                return null;
        }
    }

    private GameObject GetUnActiveWeaponSlot()
    {
        foreach (GameObject weaponSlot in WeaponManager.Instance.weaponSlots)
        {
            if (weaponSlot != WeaponManager.Instance.activeWeaponSlot)
            {
                return weaponSlot;
            }
        }

        return null;
    }

    internal void UpdateThrowables()
    {

        lethalAmountUI.text = WeaponManager.Instance.lethalsCount.ToString();
        tacticalAmountUI.text = WeaponManager.Instance.tacticalsCount.ToString();


        switch (WeaponManager.Instance.equippedLethalType)
        {
            case Throwable.ThrowableType.Grenade:
                lethalUI.sprite = Resources.Load<GameObject>("Grenade").GetComponent<SpriteRenderer>().sprite;
                break;
        }

    }
}

