using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class Player : MonoBehaviour
{
    public int HP = 100;
    public GameObject bloodyScreen;
    public TextMeshProUGUI HPText;
    public GameObject gameOverText;
    public bool isDead;

    private void Start()
    {
        HPText.text = $"{HP}";
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        HP -= damageAmount;

        if (CameraShaker.Instance != null)
        {
            
            float shakeIntensity = Mathf.Clamp(damageAmount * 0.004f, 0.05f, 0.15f);
            CameraShaker.Instance.TriggerShake(0.2f, shakeIntensity);
        }


        if (HP <= 0)
        {
            PlayerDead();
            isDead = true;
        }
        else
        {
            StartCoroutine(BloodyScreenEffect());
            SoundManager.Instance.playerChannel.PlayOneShot(SoundManager.Instance.playerHurt);
            HPText.text = $"{HP}";
        }
    }

    private void PlayerDead()
    {
        
        if (CameraShaker.Instance != null)
        {
            CameraShaker.Instance.TriggerShake(0.6f, 0.25f);
        }

        GetComponent<MouseMovement>().enabled = false;
        GetComponent<PlayerMovement>().enabled = false;
        SoundManager.Instance.playerChannel.PlayOneShot(SoundManager.Instance.playerDeath);
        SoundManager.Instance.playerChannel.clip = SoundManager.Instance.gameOversfx;
        SoundManager.Instance.playerChannel.PlayDelayed(2f);
        GetComponentInChildren<Animator>().enabled = true;
        HPText.gameObject.SetActive(false);
        GetComponent<ScreenFader>().StartFade();
        StartCoroutine(ShowGameOverText());
    }

    private IEnumerator ShowGameOverText()
    {
        yield return new WaitForSeconds(1f);
        gameOverText.gameObject.SetActive(true);
        int waveSurvived = GlobalReferences.Instance.waveNumber;
        int zombiesKilled = GlobalReferences.Instance.zombiesKilled;
        StartCoroutine(ReturnToMainMenu());
    }

    private IEnumerator ReturnToMainMenu()
    {
        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator BloodyScreenEffect()
    {
        if (bloodyScreen.activeInHierarchy == false)
        {
            bloodyScreen.SetActive(true);
        }
        var image = bloodyScreen.GetComponentInChildren<Image>();

        Color startColor = image.color;
        startColor.a = 1f;
        image.color = startColor;
        float duration = 1.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);

            Color newColor = image.color;
            newColor.a = alpha;
            image.color = newColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (bloodyScreen.activeInHierarchy)
        {
            bloodyScreen.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("ZombieHand"))
        {
            if (!isDead)
            {
                TakeDamage(other.gameObject.GetComponent<ZombieHand>().damage);
            }
        }
    }
}