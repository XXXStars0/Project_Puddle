using UnityEngine;

/// <summary>
/// A lightweight script to automatically destroy splash effect graphics after a short time.
/// </summary>
public class SplashVisual : MonoBehaviour
{
    [Tooltip("How long the splash image stays on screen before disappearing.")]
    public float lifetime = 0.3f;

    void Start()
    {
        // Automatically vanish after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }
}
