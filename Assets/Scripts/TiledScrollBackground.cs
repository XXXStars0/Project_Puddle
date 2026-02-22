using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class TiledScrollBackground : MonoBehaviour
{
    public float tileSize = 100f;
    public bool preserveAspect = true;
    public Vector2 scrollSpeed = new Vector2(0.1f, 0.1f);

    private RawImage _rawImage;
    private RectTransform _rectTransform;
    private Vector2 _currentOffset;

    void Update()
    {
        if (_rawImage == null) _rawImage = GetComponent<RawImage>();
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        if (_rawImage == null || _rectTransform == null) return;

        float canvasWidth = _rectTransform.rect.width;
        float canvasHeight = _rectTransform.rect.height;

        float uvW = tileSize > 0f ? canvasWidth / tileSize : 1f;
        float uvH = tileSize > 0f ? canvasHeight / tileSize : 1f;

        if (preserveAspect && _rawImage.texture != null)
        {
            float textureAspect = (float)_rawImage.texture.width / _rawImage.texture.height;
            uvH = tileSize > 0f ? canvasHeight / (tileSize / textureAspect) : 1f;
        }

        if (Application.isPlaying)
        {
            _currentOffset += scrollSpeed * Time.unscaledDeltaTime;
        }

        _rawImage.uvRect = new Rect(_currentOffset.x, _currentOffset.y, uvW, uvH);
    }
}