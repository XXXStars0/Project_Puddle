using UnityEngine;

/// <summary>
/// Smoothly follows the player and strictly limits the camera's view strictly to the Map Bounds.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Tooltip("Smoothing time for camera to catch up. Lower is faster.")]
    public float smoothTime = 0.15f;
    private Vector3 currentVelocity = Vector3.zero;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            Debug.LogError("[CameraController] Needs to be attached to a Camera object!");
    }

    void LateUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerTransform == null)
            return;

        Vector3 targetPos = GameManager.Instance.PlayerTransform.position;
        targetPos.z = transform.position.z; // Maintain Z-depth

        // Clamp camera cleanly to GameManager.mapBounds
        if (cam != null && GameManager.Instance.mapBounds.size != Vector3.zero)
        {
            Bounds bounds = GameManager.Instance.mapBounds;
            
            // Camera viewing dimensions in world units
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = camHalfHeight * cam.aspect;

            // Calculate strictly clamped movement limits for the camera origin
            float minX = bounds.min.x + camHalfWidth;
            float maxX = bounds.max.x - camHalfWidth;
            float minY = bounds.min.y + camHalfHeight;
            float maxY = bounds.max.y - camHalfHeight;

            // If bounds are smaller than camera view, lock to center to prevent violent jitter
            if (minX > maxX) minX = maxX = bounds.center.x;
            if (minY > maxY) minY = maxY = bounds.center.y;

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        // Apply Vector3 elastic dampening
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
    }
}
