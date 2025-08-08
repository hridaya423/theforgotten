using TMPro;
using UnityEngine;

public class GlobalReferences : MonoBehaviour
{
    public static GlobalReferences Instance { get; set; }

    public GameObject bulletImpactEffectprefab;
    public GameObject grenadeExplosionEffect;
    public GameObject smokeGrenadeEffect;
    public GameObject bloodSprayEffect;
    public GameObject zombiePrefab;
    public GameObject player;

    public int potionCount = 0;
    [SerializeField] private TextMeshProUGUI potionCountText;

    public int waveNumber;
    public int zombiesKilled = 0;

    [Header("Boss References")]
    public GameObject bossPrefab; 
    public int bossesKilled = 0;

    [Header("Essence System")]
    public int essenceCount = 0;
    public int essencesPerPotion = 4;
    [SerializeField] private AudioClip essenceCollectSound;

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

    public void IncrementBossesKilled()
    {
        bossesKilled++;
    }

    public void AddPotion(int amount = 1)
    {
        potionCount += amount;
        UpdatePotionUI();
    }

    public void AddEssence(int amount = 1)
    {
        essenceCount += amount;

        
        if (essenceCollectSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.playerChannel.PlayOneShot(essenceCollectSound);
        }

        
        

        UpdatePotionUI();
    }

    private void UpdatePotionUI()
    {
        if (potionCountText != null)
        {
            
            if (essenceCount > 0)
            {
                potionCountText.text = $"Potions: {potionCount} + ({essenceCount}/{essencesPerPotion})";
            }
            else
            {
                potionCountText.text = $"Potions: {potionCount}";
            }
        }
    }
}
