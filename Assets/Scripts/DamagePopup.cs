using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public static DamagePopup Instance;
    public GameObject damageTextPrefab;
    public bool useCanvasText = false;

    void Awake()
    {
        Instance = this;
    }

    public void Create(Vector3 position, int damage, bool isCritical = false, float heightOffset = 2f)
    {
        if (damageTextPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = position + Vector3.up * heightOffset;
        spawnPosition += new Vector3(
            Random.Range(-0.3f, 0.3f),
            0,
            Random.Range(-0.3f, 0.3f)
        );

        GameObject popup = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);
        TextMeshProUGUI tmpUI = popup.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshPro tmp3D = popup.GetComponentInChildren<TextMeshPro>();

        if (tmpUI != null)
        {
            tmpUI.text = damage.ToString();
            tmpUI.fontSize = isCritical ? 48 : 36;
            tmpUI.color = isCritical ? Color.yellow : Color.white;
            tmpUI.fontStyle = isCritical ? FontStyles.Bold : FontStyles.Normal;
        }
        else if (tmp3D != null)
        {
            tmp3D.text = damage.ToString();
            tmp3D.fontSize = isCritical ? 12 : 8;
            tmp3D.color = isCritical ? Color.yellow : Color.white;
            tmp3D.fontStyle = isCritical ? FontStyles.Bold : FontStyles.Normal;
        }

        FloatingText floater = popup.GetComponent<FloatingText>();
        if (floater == null)
            floater = popup.AddComponent<FloatingText>();

        Destroy(popup, 1.5f);
    }
}