using UnityEngine;

/// <summary>
/// A generic power-up that the Cloud can collect.
/// Requires a Collider2D set to Trigger.
/// </summary>
public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        Water,
        Speed,
        DarkCloud
    }

    [Header("Power Up Settings")]
    public PowerUpType type = PowerUpType.DarkCloud;
    public float amount = 20f; // Amount to enhance

    [Header("Visuals")]
    public GameObject collectEffect; // Particle/Sound effect on collection

    private void Start()
    {
        // Ensure Physics overlap works between Trigger and Cloud's Kinematic Rigidbody
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the cloud collected it (search parent too in case the collider is on a child visual object)
        CloudController cloud = collision.GetComponentInParent<CloudController>();
        if (cloud != null)
        {
            cloud.CollectPowerUp(type.ToString(), amount);

            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject); // Self destruct after collection
        }
    }
}
