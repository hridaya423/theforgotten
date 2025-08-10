using TMPro;
using UnityEngine;

public class GlobalReferences : MonoBehaviour
{
    public static GlobalReferences Instance { get; set; }

    public GameObject bulletImpactEffectprefab;
    public GameObject grenadeExplosionEffect;
    public GameObject bloodSprayEffect;
    public GameObject zombiePrefab;
    public GameObject player;

    public int potionCount = 0;
    [SerializeField] private TextMeshProUGUI potionCountText;

    public int waveNumber;
    public int zombiesKilled = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void IncrementWave()
    {
        waveNumber++;
    }

    public void IncrementZombiesKilled()
    {
        zombiesKilled++;
    }
}
