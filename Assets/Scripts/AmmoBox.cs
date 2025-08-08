using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    public int ammoAmount = 200;
    public AmmoType ammoType;

    public enum AmmoType
    {
        PistolAmmo,
        RifleAmmo,
        ShotgunAmmo,
        SMGAmmo
    }
}

