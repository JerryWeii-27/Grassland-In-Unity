using TMPro;
using UnityEngine;

public class DisplayFPS : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;

    private void Start()
    {
        fpsText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        fpsText.text = $"FPS: {Mathf.Round(fps)} \nDelta time: {deltaTime * 1000} ms";
    }
}
