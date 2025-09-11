using UnityEngine;

public class PortalEffect : MonoBehaviour
{
    private Material portalMaterial;
    private float animationTime = 0f;
    private Renderer portalRenderer;

    public void Initialize(Color baseColor)
    {
        portalRenderer = GetComponent<Renderer>();
        portalMaterial = portalRenderer.material;
        portalMaterial.color = baseColor;

        // Enable emission
        portalMaterial.EnableKeyword("_EMISSION");
        portalMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        StartCoroutine(AnimatePortal());
    }

    private System.Collections.IEnumerator AnimatePortal()
    {
        while (true)
        {
            animationTime += Time.deltaTime;

            // Animate the portal material with more complex effects
            float noiseScale = Mathf.PerlinNoise(animationTime * 0.5f, 0) * 2f + 1f;
            float distortion = Mathf.Sin(animationTime) * 0.1f;
            float pulse = Mathf.PingPong(animationTime * 0.3f, 1f) * 2f;

            // Set material properties
            portalMaterial.SetFloat("_NoiseScale", noiseScale);
            portalMaterial.SetFloat("_Distortion", distortion);
            portalMaterial.SetFloat("_Pulse", pulse);

            // Animate emission
            float emissionIntensity = (Mathf.Sin(animationTime) * 0.5f + 0.5f) * 2f + 1f;
            Color emissionColor = portalMaterial.color * emissionIntensity;
            portalMaterial.SetColor("_EmissionColor", emissionColor);

            // Dynamic light emission for real-time lighting
            portalRenderer.UpdateGIMaterials();
            DynamicGI.SetEmissive(portalRenderer, emissionColor);

            yield return null;
        }
    }

    void OnDestroy()
    {
        if (portalMaterial != null)
        {
            Destroy(portalMaterial);
        }
    }
}