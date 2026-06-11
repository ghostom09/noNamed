using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedSprite
{
    [SerializeField] private Sprite sprite;
    [SerializeField, Min(1)] private int weight = 1;

    public Sprite Sprite => sprite;
    public int Weight => Mathf.Max(1, weight);
}

[CreateAssetMenu(menuName = "Dungeon/Room Skin")]
public class RoomSkin : ScriptableObject
{
    [Header("Floor")]
    [SerializeField] private List<WeightedSprite> floorTiles = new List<WeightedSprite>();
    [SerializeField] private string floorSortingLayerName = "Default";
    [SerializeField] private int floorOrderInLayer = 0;

    [Header("Border")]
    [SerializeField] private Sprite borderTop;
    [SerializeField] private Sprite borderBottom;
    [SerializeField] private Sprite borderLeft;
    [SerializeField] private Sprite borderRight;
    [SerializeField] private Sprite cornerTopLeft;
    [SerializeField] private Sprite cornerTopRight;
    [SerializeField] private Sprite cornerBottomLeft;
    [SerializeField] private Sprite cornerBottomRight;
    [SerializeField] private string borderSortingLayerName = "Default";
    [SerializeField] private int borderOrderInLayer = 10;

    [Header("Door Frame")]
    [SerializeField] private Sprite doorFrameHorizontal;
    [SerializeField] private Sprite doorFrameVertical;
    [SerializeField] private string doorFrameSortingLayerName = "Default";
    [SerializeField] private int doorFrameOrderInLayer = 11;

    public IReadOnlyList<WeightedSprite> FloorTiles => floorTiles;
    public string FloorSortingLayerName => floorSortingLayerName;
    public int FloorOrderInLayer => floorOrderInLayer;

    public Sprite BorderTop => borderTop;
    public Sprite BorderBottom => borderBottom;
    public Sprite BorderLeft => borderLeft;
    public Sprite BorderRight => borderRight;
    public Sprite CornerTopLeft => cornerTopLeft;
    public Sprite CornerTopRight => cornerTopRight;
    public Sprite CornerBottomLeft => cornerBottomLeft;
    public Sprite CornerBottomRight => cornerBottomRight;
    public string BorderSortingLayerName => borderSortingLayerName;
    public int BorderOrderInLayer => borderOrderInLayer;

    public Sprite DoorFrameHorizontal => doorFrameHorizontal;
    public Sprite DoorFrameVertical => doorFrameVertical;
    public string DoorFrameSortingLayerName => doorFrameSortingLayerName;
    public int DoorFrameOrderInLayer => doorFrameOrderInLayer;

    public Sprite GetRandomFloorSprite(System.Random random)
    {
        if (floorTiles == null || floorTiles.Count == 0)
            return null;

        var totalWeight = 0;
        for (var i = 0; i < floorTiles.Count; i++)
        {
            var tile = floorTiles[i];
            if (tile == null || tile.Sprite == null)
                continue;

            totalWeight += tile.Weight;
        }

        if (totalWeight <= 0)
            return null;

        var value = random.Next(0, totalWeight);
        for (var i = 0; i < floorTiles.Count; i++)
        {
            var tile = floorTiles[i];
            if (tile == null || tile.Sprite == null)
                continue;

            value -= tile.Weight;
            if (value < 0)
                return tile.Sprite;
        }

        return null;
    }
}