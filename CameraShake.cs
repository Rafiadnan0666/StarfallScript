using UnityEngine;

// Camera shake for earthquake effect
public class CameraShake : MonoBehaviour
{
    private float intensity;
    private float frequency;
    private Vector3 originalPosition;
    private float trauma = 0f;

    public void SetParameters(float intensity, float frequency)
    {
        this.intensity = intensity;
        this.frequency = frequency;
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        if (trauma > 0)
        {
            float shake = trauma * trauma;
            Vector3 offset = new Vector3(
                Mathf.PerlinNoise(0, Time.time * frequency) * 2f - 1f,
                Mathf.PerlinNoise(1, Time.time * frequency) * 2f - 1f,
                Mathf.PerlinNoise(2, Time.time * frequency) * 2f - 1f
            ) * intensity * shake;

            transform.localPosition = originalPosition + offset;
            trauma = Mathf.Clamp01(trauma - Time.deltaTime);
        }
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }
}