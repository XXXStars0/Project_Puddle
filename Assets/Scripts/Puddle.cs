using UnityEngine;

/// <summary>
/// Controls puddle size, evaporation, and visual representation.
/// </summary>
public class Puddle : MonoBehaviour
{
    [Header("Size Settings")]
    public float startSize = 1f;
    public float maxSize = 5f;
    public float currentSize;

    [Header("Environmental Effects")]
    public float evaporateRate = 0.2f; // Size lost per second automatically
    
    // Future interface for weather like sunny day
    public float sunEvaporationMultiplier = 1.0f; 

    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
        currentSize = startSize;
        UpdateVisuals();
    }

    private void Update()
    {
        // Natural dry-up over time -> shrinks puddle
        float decay = evaporateRate * sunEvaporationMultiplier * Time.deltaTime;
        ModifySize(-decay);
    }

    /// <summary>
    /// Interface for other elements (rain, NPCs, sun) to change puddle size.
    /// </summary>
    public void ModifySize(float amount)
    {
        currentSize += amount;

        if (currentSize <= 0f)
        {
            Debug.Log("Puddle dried up and vanished.");
            Destroy(gameObject);
            return;
        }

        // Clamp to max size
        currentSize = Mathf.Min(currentSize, maxSize);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Scale proportionally based on currentSize
        transform.localScale = initialScale * currentSize;
        
        // Interface: could also fade alpha (transparency) as it gets smaller
    }

    // Interface for Environmental changes (e.g. Sunny Weather event)
    public void SetSunIntensity(float multiplier)
    {
        sunEvaporationMultiplier = multiplier;
    }
}
