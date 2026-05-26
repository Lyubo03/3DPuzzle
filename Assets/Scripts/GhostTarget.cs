using UnityEngine;

public class GhostTarget : MonoBehaviour
{
    [Header("Pulse Effect")]
    public bool enablePulse = true;
    public float pulseSpeed = 2f;
    public float minAlpha = 0.15f;
    public float maxAlpha = 0.4f;

    private Renderer ghostRenderer;
    private Material ghostMaterial;

    void Start()
    {
        ghostRenderer = GetComponent<Renderer>();
        if (ghostRenderer != null)
        {
            // Create instance material so each ghost pulses independently
            ghostMaterial = ghostRenderer.material;
            SetupTransparentMaterial();
        }
    }

    void Update()
    {
        if (!enablePulse || ghostMaterial == null) return;

        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        Color c = ghostMaterial.color;
        c.a = alpha;
        ghostMaterial.color = c;
    }

    void SetupTransparentMaterial()
    {
        // Set material to transparent rendering mode
        ghostMaterial.SetFloat("_Mode", 3); // Transparent
        ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetInt("_ZWrite", 0);
        ghostMaterial.DisableKeyword("_ALPHATEST_ON");
        ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
        ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ghostMaterial.renderQueue = 3000;

        Color c = ghostMaterial.color;
        c.a = maxAlpha;
        ghostMaterial.color = c;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
