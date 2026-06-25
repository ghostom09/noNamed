using UnityEngine;

public class EliteBorderRenderer : MonoBehaviour
{
    private static readonly Color BorderColor = new(1f, 0f, 0f, 1f);

    private SpriteRenderer _source;
    private SpriteRenderer _border;

    public void Initialize(SpriteRenderer source, float scaleMultiplier)
    {
        _source = source;

        GameObject borderObject = new("Elite Border");
        borderObject.transform.SetParent(source.transform, false);
        borderObject.transform.localScale = Vector3.one * scaleMultiplier;

        _border = borderObject.AddComponent<SpriteRenderer>();
        SyncRenderer();
    }

    private void LateUpdate()
    {
        SyncRenderer();
    }

    private void SyncRenderer()
    {
        if (_source == null || _border == null)
            return;

        _border.sprite = _source.sprite;
        _border.flipX = _source.flipX;
        _border.flipY = _source.flipY;
        _border.drawMode = _source.drawMode;
        _border.size = _source.size;
        _border.maskInteraction = _source.maskInteraction;
        _border.sortingLayerID = _source.sortingLayerID;
        _border.sortingOrder = _source.sortingOrder - 1;
        _border.sharedMaterial = _source.sharedMaterial;
        _border.color = BorderColor;
        _border.enabled = _source.enabled;
    }
}
