using UnityEngine;

/// <summary>
/// Displays a bar above the NPC's head to visualize emotion (0–1).
/// Attach to a child of the NPC; assign Background and Fill (SpriteRenderers or RectTransforms).
/// </summary>
public class NPCEmotionBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to use parent NPCBase")]
    public NPCBase npc;
    [Tooltip("Background bar (full width).")]
    public Transform background;
    [Tooltip("Fill bar (will be scaled by emotion).")]
    public Transform fill;

    [Header("Layout")]
    [Tooltip("Check to only show fill bar, hide background.")]
    public bool fillOnly = false;
    [Tooltip("Height above NPC pivot")]
    public float heightOffset = 1.2f;
    [Tooltip("Bar width in world units (used if fill pivot is center)")]
    public float barWidth = 1.5f;

    [Header("Colors (optional, for SpriteRenderer only)")]
    public bool useColorGradient = true;
    public Color colorLow = new Color(0.9f, 0.2f, 0.2f);
    public Color colorMid = new Color(0.95f, 0.85f, 0.2f);
    public Color colorHigh = new Color(0.2f, 0.85f, 0.3f);

    private SpriteRenderer _fillSprite;
    private float _lastRatio = -1f;
    /// <summary>Cached Fill localScale.x when at "full" (1.0) so bar width matches Background.</summary>
    private float _fillFullScaleX = 1f;

    private void Awake()
    {
        if (npc == null)
            npc = GetComponentInParent<NPCBase>();

        if (fill != null)
        {
            _fillSprite = fill.GetComponent<SpriteRenderer>();
            _fillFullScaleX = fill.localScale.x;
        }

        if (background != null && fillOnly)
            background.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (npc == null) return;

        // Keep bar above NPC
        transform.position = npc.transform.position + Vector3.up * heightOffset;

        float ratio = npc.GetEmotionRatio();
        if (fill != null)
        {
            float r = Mathf.Clamp01(ratio);
            Vector3 scale = fill.localScale;
            scale.x = r * _fillFullScaleX; // same full width as Background so left/right edges align
            fill.localScale = scale;

            Vector3 pos = fill.localPosition;
            pos.x = -(1f - r) * (_fillFullScaleX * 0.5f); // left-edge align with Background (pivot center)
            fill.localPosition = pos;

            if (useColorGradient && _fillSprite != null)
                _fillSprite.color = GetColorForRatio(ratio);

            _lastRatio = ratio;
        }
    }

    private Color GetColorForRatio(float r)
    {
        if (r <= 0.5f)
            return Color.Lerp(colorLow, colorMid, r * 2f);
        return Color.Lerp(colorMid, colorHigh, (r - 0.5f) * 2f);
    }
}
