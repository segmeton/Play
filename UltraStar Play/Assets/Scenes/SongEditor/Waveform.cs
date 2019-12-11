using UnityEngine;
using UnityEngine.UI;

public class Waveform : MonoBehaviour
{
    private RectTransform rectTransform;
    private Texture2D texture;
    private RawImage rawImage;
    private Material material;
    private float[] samples = new float[1023];

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();

        material = rawImage.material;
        if (material == null)
        {
            Debug.LogError("no material");
        }

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)i / samples.Length;
            // Debug.Log(samples[i]);
        }
        material.SetFloatArray("_samples", samples);
    }

    void Update()
    {
        // for (int i = 0; i < samples.Length; i++)
        // {
        //     samples[i] = Mathf.Sin(Time.time + i * 1f) / 2 + 0.5f;
        // }
        // material.SetFloatArray("_samples", samples);
    }

    private void CreateTexture()
    {
        int textureWidth = (int)rectTransform.rect.width;
        int textureHeight = (int)rectTransform.rect.height;

        // create the texture and assign to the guiTexture: 
        texture = new Texture2D(textureWidth, textureHeight);
        rawImage.texture = texture;
    }
}