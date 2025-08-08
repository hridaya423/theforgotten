using UnityEngine;

public class PlayerCollisionEffects : MonoBehaviour
{
    public enum ObjectType
    {
        Tower,
        Scientist
    }

    [Header("Mode Selection")]
    [SerializeField] private ObjectType objectType = ObjectType.Tower;

    [Header("Effect Settings")]
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private float effectDuration = 2f;
    [SerializeField] private bool usePooling = false;
    [SerializeField] private int poolSize = 10;

    [Header("Collision Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float cooldownTime = 0.2f;
    [SerializeField] private LayerMask collisionLayers;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip collisionSound;
    [SerializeField] private float volume = 0.7f;

    
    private GameObject[] effectPool;
    private int poolIndex = 0;
    private float lastEffectTime = 0f;
    private AudioSource audioSource;
    private Collider myCollider;
    private bool colliderDisabled = false;

    private void Start()
    {
        
        myCollider = GetComponent<Collider>();

        
        if (collisionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; 
            audioSource.volume = volume;
            audioSource.clip = collisionSound;
        }

        
        if (usePooling && effectPrefab != null)
        {
            InitializeObjectPool();
        }
    }

    private void Update()
    {
        if (colliderDisabled)
            return;

        if (GlobalReferences.Instance == null)
            return;

        bool shouldDisable = false;

        
        switch (objectType)
        {
            case ObjectType.Tower:
                shouldDisable = GlobalReferences.Instance.essenceCount >= 4;
                break;

            case ObjectType.Scientist:
                shouldDisable = GlobalReferences.Instance.potionCount >= 1;
                break;
        }

        
        if (shouldDisable && myCollider != null)
        {
            myCollider.enabled = false;
            colliderDisabled = true;
            
        }
    }

    private void InitializeObjectPool()
    {
        effectPool = new GameObject[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            effectPool[i] = Instantiate(effectPrefab, Vector3.zero, Quaternion.identity);
            effectPool[i].SetActive(false);
            effectPool[i].transform.SetParent(transform);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ShouldSkipCollision())
            return;

        HandleCollision(collision.gameObject, collision.GetContact(0).point);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ShouldSkipCollision())
            return;

        
        Vector3 collisionPoint = other.ClosestPoint(transform.position);
        HandleCollision(other.gameObject, collisionPoint);
    }

    private bool ShouldSkipCollision()
    {
        if (GlobalReferences.Instance == null)
            return false;

        switch (objectType)
        {
            case ObjectType.Tower:
                return GlobalReferences.Instance.essenceCount >= 4;

            case ObjectType.Scientist:
                return GlobalReferences.Instance.potionCount >= 1;

            default:
                return false;
        }
    }

    private void HandleCollision(GameObject collidingObject, Vector3 hitPoint)
    {
        
        if (collidingObject.CompareTag(playerTag))
        {
            
            if (Time.time - lastEffectTime < cooldownTime)
                return;

            lastEffectTime = Time.time;

            
            if (audioSource != null && collisionSound != null)
            {
                audioSource.PlayOneShot(collisionSound);
            }

            
            SpawnEffect(hitPoint);
        }
    }

    private void SpawnEffect(Vector3 position)
    {
        GameObject effect;

        if (usePooling)
        {
            
            effect = effectPool[poolIndex];
            effect.transform.position = position;
            effect.SetActive(true);

            
            StartCoroutine(DeactivateAfterDelay(effect));

            
            poolIndex = (poolIndex + 1) % poolSize;
        }
        else if (effectPrefab != null)
        {
            
            effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
    }

    private System.Collections.IEnumerator DeactivateAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(effectDuration);
        obj.SetActive(false);
    }
}
