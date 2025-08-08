using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float floatSpeed = 2f;
    public float fadeSpeed = 0.8f;
    public float sideMovement = 0.3f;

    private TextMeshProUGUI textUI;
    private TextMeshPro text3D;
    private Vector3 velocity;
    private Camera cam;

    void Start()
    {
        textUI = GetComponentInChildren<TextMeshProUGUI>();
        text3D = GetComponentInChildren<TextMeshPro>();
        cam = Camera.main;

        velocity = new Vector3(
            Random.Range(-sideMovement, sideMovement),
            floatSpeed,
            Random.Range(-sideMovement, sideMovement)
        );

        FaceCamera();
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
        velocity *= (1f - Time.deltaTime * 2f);
        FaceCamera();

        if (textUI != null)
        {
            Color c = textUI.color;
            c.a -= fadeSpeed * Time.deltaTime;
            textUI.color = c;
        }
        else if (text3D != null)
        {
            Color c = text3D.color;
            c.a -= fadeSpeed * Time.deltaTime;
            text3D.color = c;
        }

        transform.localScale *= (1f - Time.deltaTime * 0.2f);
    }

    void FaceCamera()
    {
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                           cam.transform.rotation * Vector3.up);
        }
    }
}