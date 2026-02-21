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

    private void Awake()
    {
        if (npc == null)
            npc = GetComponentInParent<NPCBase>();

        if (fill != null)
            _fillSprite = fill.GetComponent<SpriteRenderer>();

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
            scale.x = r;
            fill.localScale = scale;
            // If fill pivot is center: shift so bar fills from left
            fill.localPosition = new Vector3(-(1f - r) * (barWidth * 0.5f), 0f, 0f);

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
