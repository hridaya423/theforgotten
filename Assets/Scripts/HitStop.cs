using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance;
    private bool isWaiting = false;

    void Awake()
    {
        Instance = this;
    }

    public void Stop(float duration)
    {
        if (!isWaiting)
            StartCoroutine(StopTime(duration));
    }

    IEnumerator StopTime(float duration)
    {
        isWaiting = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        isWaiting = false;
    }
}