using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KillStreakManager : MonoBehaviour
{
    public static KillStreakManager Instance;

    [Header("Killstreak Settings")]
    [SerializeField] private float comboTimeWindow = 3f;
    [SerializeField] private float bonusDuration = 10f; 

    [Header("Damage Multipliers")]
    [SerializeField] private float doubleDamageMultiplier = 1.5f;
    [SerializeField] private float tripleDamageMultiplier = 2f;
    [SerializeField] private float megaDamageMultiplier = 2.5f;
    [SerializeField] private float ultraDamageMultiplier = 3f;

    [Header("Killstreak Thresholds")]
    [SerializeField] private int doubleKillThreshold = 2;
    [SerializeField] private int tripleKillThreshold = 3;
    [SerializeField] private int megaKillThreshold = 5;
    [SerializeField] private int ultraKillThreshold = 8;
    [SerializeField] private int godlikeKillThreshold = 12;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private Image multiplierTimerBar;
    [SerializeField] private GameObject multiplierPanel;

    [Header("Slow Motion Settings")]
    [SerializeField] private bool enableKillstreakSlowMo = true;
    [SerializeField] private float killstreakSlowMoScale = 0.3f;
    [SerializeField] private float killstreakSlowMoDuration = 0.5f;
    [SerializeField] private float godlikeSlowMoDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip comboSound;
    [SerializeField] private AudioClip bonusEndSound;

    
    private int currentKillstreak = 0;
    private float lastKillTime = 0f;
    private float currentDamageMultiplier = 1f;
    private Coroutine damageMultiplierCoroutine;
    private GameObject player;
    private Dictionary<Weapon, int> originalWeaponDamages = new Dictionary<Weapon, int>();

    
    private readonly Color doubleKillColor = Color.yellow;
    private readonly Color tripleKillColor = new Color(1f, 0.5f, 0f); 
    private readonly Color megaKillColor = Color.red;
    private readonly Color ultraKillColor = new Color(0.5f, 0f, 1f); 
    private readonly Color godlikeColor = new Color(1f, 0f, 1f); 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (multiplierPanel != null)
        {
            multiplierPanel.SetActive(false);
        }
    }

    public void RegisterKill(Vector3 position, bool isHeadshot = false)
    {
        float currentTime = Time.time;

        
        if (currentTime - lastKillTime <= comboTimeWindow)
        {
            currentKillstreak++;
        }
        else
        {
            
            currentKillstreak = 1;
        }

        lastKillTime = currentTime;

        
        CheckKillstreakMilestones(position, isHeadshot);
    }

    private void CheckKillstreakMilestones(Vector3 position, bool isHeadshot)
    {
        string streakText = "";
        Color textColor = Color.white;
        float newMultiplier = 1f;
        bool showCombo = false;
        float slowMoDuration = 0f;

        if (currentKillstreak >= godlikeKillThreshold)
        {
            streakText = "GODLIKE!!!";
            textColor = godlikeColor;
            newMultiplier = ultraDamageMultiplier;
            showCombo = true;
            slowMoDuration = godlikeSlowMoDuration;
        }
        else if (currentKillstreak >= ultraKillThreshold)
        {
            streakText = "ULTRA KILL!!";
            textColor = ultraKillColor;
            newMultiplier = ultraDamageMultiplier;
            showCombo = true;
            slowMoDuration = killstreakSlowMoDuration * 1.5f;
        }
        else if (currentKillstreak >= megaKillThreshold)
        {
            streakText = "MEGA KILL!";
            textColor = megaKillColor;
            newMultiplier = megaDamageMultiplier;
            showCombo = true;
            slowMoDuration = killstreakSlowMoDuration;
        }
        else if (currentKillstreak >= tripleKillThreshold)
        {
            streakText = "TRIPLE KILL!";
            textColor = tripleKillColor;
            newMultiplier = tripleDamageMultiplier;
            showCombo = true;
            slowMoDuration = killstreakSlowMoDuration * 0.8f;
        }
        else if (currentKillstreak >= doubleKillThreshold)
        {
            streakText = "DOUBLE KILL!";
            textColor = doubleKillColor;
            newMultiplier = doubleDamageMultiplier;
            showCombo = true;
            slowMoDuration = killstreakSlowMoDuration * 0.6f;
        }

        
        if (showCombo && DamagePopup.Instance != null)
        {
            
            ShowComboPopup(position, streakText, textColor);

            
            if (enableKillstreakSlowMo && slowMoDuration > 0)
            {
                StartCoroutine(KillstreakSlowMotion(slowMoDuration, textColor));
            }

            
            if (comboSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.zombieChannel1.PlayOneShot(comboSound);
            }

            
            if (newMultiplier > currentDamageMultiplier)
            {
                ApplyDamageMultiplier(newMultiplier);
            }
        }
    }

    private IEnumerator KillstreakSlowMotion(float duration, Color effectColor)
    {
        
        if (Time.timeScale < 0.9f) yield break;

        
        Time.timeScale = killstreakSlowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        
        if (CameraShaker.Instance != null)
        {
            CameraShaker.Instance.ShakeLight();
        }

        

        
        yield return new WaitForSecondsRealtime(duration);

        
        float elapsed = 0f;
        float returnDuration = 0.2f;
        float startScale = Time.timeScale;

        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(startScale, 1f, elapsed / returnDuration);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private void ShowComboPopup(Vector3 worldPosition, string text, Color color)
    {
        if (DamagePopup.Instance == null) return;

        
        GameObject popup = new GameObject("ComboPopup");
        popup.transform.position = worldPosition + Vector3.up * 3f;

        
        Canvas canvas = popup.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.transform.localScale = Vector3.one * 0.01f;

        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(popup.transform, false);
        TextMeshProUGUI comboText = textObj.AddComponent<TextMeshProUGUI>();
        comboText.text = text;
        comboText.fontSize = 72;
        comboText.color = color;
        comboText.alignment = TextAlignmentOptions.Center;
        comboText.fontStyle = FontStyles.Bold;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 100);

        
        StartCoroutine(AnimateComboPopup(popup, comboText));
    }

    private IEnumerator AnimateComboPopup(GameObject popup, TextMeshProUGUI text)
    {
        float duration = 2f;
        float elapsed = 0f;
        Vector3 startPos = popup.transform.position;
        Color startColor = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            
            popup.transform.position = startPos + Vector3.up * (t * 2f);

            
            float scale = 1f + (t * 0.5f);
            popup.transform.localScale = Vector3.one * 0.01f * scale;

            
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) * 2f;
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
            }

            yield return null;
        }

        Destroy(popup);
    }

    private void ApplyDamageMultiplier(float multiplier)
    {
        currentDamageMultiplier = multiplier;

        
        if (damageMultiplierCoroutine != null)
        {
            StopCoroutine(damageMultiplierCoroutine);
        }

        
        UpdateWeaponDamage(multiplier);

        
        ShowMultiplierUI(multiplier);

        
        damageMultiplierCoroutine = StartCoroutine(DamageMultiplierTimer());
    }

    private void UpdateWeaponDamage(float multiplier)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
        }

        
        Weapon[] weapons = player.GetComponentsInChildren<Weapon>(true);

        foreach (Weapon weapon in weapons)
        {
            
            if (!originalWeaponDamages.ContainsKey(weapon))
            {
                
                originalWeaponDamages[weapon] = weapon.weaponDamage;
            }

            
            weapon.weaponDamage = Mathf.RoundToInt(originalWeaponDamages[weapon] * multiplier);
        }
    }

    private void ResetWeaponDamage()
    {
        
        foreach (var kvp in originalWeaponDamages)
        {
            if (kvp.Key != null)
            {
                kvp.Key.weaponDamage = kvp.Value;
            }
        }
    }

    private void ShowMultiplierUI(float multiplier)
    {
        if (multiplierPanel != null)
        {
            multiplierPanel.SetActive(true);
        }

        if (multiplierText != null)
        {
            multiplierText.text = $"DAMAGE x{multiplier:F1}";
            multiplierText.color = GetMultiplierColor(multiplier);
        }

        if (multiplierTimerBar != null)
        {
            multiplierTimerBar.fillAmount = 1f;
            multiplierTimerBar.color = GetMultiplierColor(multiplier);
        }
    }

    private Color GetMultiplierColor(float multiplier)
    {
        if (multiplier >= ultraDamageMultiplier) return ultraKillColor;
        if (multiplier >= megaDamageMultiplier) return megaKillColor;
        if (multiplier >= tripleDamageMultiplier) return tripleKillColor;
        if (multiplier >= doubleDamageMultiplier) return doubleKillColor;
        return Color.white;
    }

    private IEnumerator DamageMultiplierTimer()
    {
        float elapsed = 0f;

        while (elapsed < bonusDuration)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - (elapsed / bonusDuration);

            if (multiplierTimerBar != null)
            {
                multiplierTimerBar.fillAmount = remaining;
            }

            
            if (remaining < 0.2f && multiplierText != null)
            {
                float flash = Mathf.Sin(Time.time * 10f);
                Color baseColor = GetMultiplierColor(currentDamageMultiplier);
                multiplierText.color = Color.Lerp(baseColor, Color.white, flash * 0.5f + 0.5f);
            }

            yield return null;
        }

        
        currentDamageMultiplier = 1f;
        ResetWeaponDamage();

        if (multiplierPanel != null)
        {
            multiplierPanel.SetActive(false);
        }

        
        if (bonusEndSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.zombieChannel1.PlayOneShot(bonusEndSound);
        }

        
        currentKillstreak = 0;
        damageMultiplierCoroutine = null;
    }

    public float GetCurrentDamageMultiplier()
    {
        return currentDamageMultiplier;
    }

    public int GetCurrentKillstreak()
    {
        return currentKillstreak;
    }

    private void OnDestroy()
    {
        
        if (damageMultiplierCoroutine != null)
        {
            StopCoroutine(damageMultiplierCoroutine);
        }
        ResetWeaponDamage();
    }
}