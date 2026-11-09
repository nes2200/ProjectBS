using UnityEngine;

public class SpawnFade : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float duration = 0.5f;

    static readonly int FadeId = Shader.PropertyToID("_Fade");

    Renderer[] targetRenderers;
    MaterialPropertyBlock propertyBlock;

    float elapsed;
    bool isPlaying;

    void Awake()
    {
        targetRenderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Play()
    {
        elapsed = 0f;
        isPlaying = true;
        SetFade(0f);
    }

    void Update()
    {
        if (!isPlaying) return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(
            elapsed / Mathf.Max(0.01f, duration));

        SetFade(Mathf.SmoothStep(0f, 1f, progress));

        if (progress >= 1f)
            isPlaying = false;
    }

    void OnDisable()
    {
        isPlaying = false;
        SetFade(1f);
    }

    void SetFade(float value)
    {
        if (propertyBlock == null) return;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (!targetRenderer) continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FadeId, value);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}