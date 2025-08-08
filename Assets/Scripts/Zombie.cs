using UnityEngine;
using System.Collections;

public class Zombie : MonoBehaviour
{
    public ZombieHand zombieHand;
    public int zombieDamage;
    public Canvas Mark;

    private Camera minimapCamera;

    private void Start()
    {
        zombieHand.damage = zombieDamage;
        StartCoroutine(FindCamera());
    }

    private IEnumerator FindCamera()
    {
        while (minimapCamera == null)
        {
            GameObject cameraObj = GameObject.FindGameObjectWithTag("MinimapCamera");
            if (cameraObj != null)
            {
                minimapCamera = cameraObj.GetComponent<Camera>();
                if (Mark != null)
                {
                    Mark.worldCamera = minimapCamera;
                }
                yield break; 
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}