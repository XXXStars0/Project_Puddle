using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("Smoothing time. Lower is faster.")]
    public float smoothTime = 0.15f;
    private Vector3 currentVelocity = Vector3.zero;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        // if (cam == null) Debug.LogError("[CameraController] Needs a Camera object!");
    }

    void LateUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerTransform == null)
            return;

        Vector3 targetPos = GameManager.Instance.PlayerTransform.position;
        targetPos.z = transform.position.z;

        if (cam != null && GameManager.Instance.mapBounds.size != Vector3.zero)
        {
            Bounds bounds = GameManager.Instance.mapBounds;
            
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = camHalfHeight * cam.aspect;

            float minX = bounds.min.x + camHalfWidth;
            float maxX = bounds.max.x - camHalfWidth;
            float minY = bounds.min.y + camHalfHeight;
            float maxY = bounds.max.y - camHalfHeight;

            if (minX > maxX) minX = maxX = bounds.center.x;
            if (minY > maxY) minY = maxY = bounds.center.y;

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
    }
}
