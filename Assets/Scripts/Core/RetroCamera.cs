using UnityEngine;

/// <summary>
/// Renders the 3D view at a low resolution and scales it up with crisp, square pixels for the
/// chunky 90s look. Press P to toggle. (Works in the Built-in render pipeline, which this
/// project uses; elsewhere the image simply stays at full resolution.)
/// </summary>
[RequireComponent(typeof(Camera))]
public class RetroCamera : MonoBehaviour
{
    public static bool Pixelate = true;
    public int targetHeight = 270;

    RenderTexture low;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!Pixelate)
        {
            Graphics.Blit(source, destination);
            return;
        }
        int h = Mathf.Min(targetHeight, source.height);
        int w = Mathf.Max(1, Mathf.RoundToInt(h * (float)source.width / source.height));
        if (!low || low.width != w || low.height != h)
        {
            if (low) low.Release();
            low = new RenderTexture(w, h, 0, source.format) { filterMode = FilterMode.Point };
        }
        Graphics.Blit(source, low);
        Graphics.Blit(low, destination);
    }

    void OnDestroy()
    {
        if (low) low.Release();
    }
}
