using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; set; }
    public Weapon hoveredWeapon = null;
    public AmmoBox hoveredAmmoBox = null;
    public Throwable hoveredThrowable = null;

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
        
        bool foundInteractable = false;
        Weapon currentWeapon = null;
        AmmoBox currentAmmoBox = null;
        Throwable currentThrowable = null;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            GameObject objecthitbyraycast = hit.transform.gameObject;

            
            Weapon weaponComponent = objecthitbyraycast.GetComponent<Weapon>();
            if (weaponComponent && !weaponComponent.isActiveWeapon)
            {
                currentWeapon = weaponComponent;
                foundInteractable = true;
                currentWeapon.GetComponent<Outline>().enabled = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    WeaponManager.Instance.PickupWeapon(currentWeapon.gameObject);
                }
            }

            
            AmmoBox ammoComponent = objecthitbyraycast.GetComponent<AmmoBox>();
            if (ammoComponent)
            {
                currentAmmoBox = ammoComponent;
                foundInteractable = true;
                currentAmmoBox.GetComponent<Outline>().enabled = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    WeaponManager.Instance.PickupAmmo(currentAmmoBox);
                    Destroy(currentAmmoBox.gameObject);
                }
            }

            
            Throwable throwableComponent = objecthitbyraycast.GetComponent<Throwable>();
            if (throwableComponent)
            {
                currentThrowable = throwableComponent;
                foundInteractable = true;
                currentThrowable.GetComponent<Outline>().enabled = true;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    WeaponManager.Instance.PickupThrowable(currentThrowable);
                }
            }

            
          
        }

        
        if (!foundInteractable)
        {
            if (hoveredWeapon)
            {
                hoveredWeapon.GetComponent<Outline>().enabled = false;
            }
            if (hoveredAmmoBox)
            {
                hoveredAmmoBox.GetComponent<Outline>().enabled = false;
            }
            if (hoveredThrowable)
            {
                hoveredThrowable.GetComponent<Outline>().enabled = false;
            }
           
        }

        
        hoveredWeapon = currentWeapon;
        hoveredAmmoBox = currentAmmoBox;
        hoveredThrowable = currentThrowable;
    }
}
