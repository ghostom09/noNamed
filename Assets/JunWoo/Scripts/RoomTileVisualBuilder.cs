using System.Collections.Generic;
using UnityEngine;

public class RoomTileVisualBuilder : MonoBehaviour
{
    [Header("Build")]
    [SerializeField] private bool buildOnRoomInit = true;
    [SerializeField] private bool clearBeforeBuild = true;
    [SerializeField] private Vector2 tileWorldSize = Vector2.one;
    [SerializeField] private bool fitSpriteToTileSize = true;
    [SerializeField] private bool useRoomIdAsRandomSeed = true;
    [SerializeField] private int extraRandomSeed;

    [Header("Floor")]
    [SerializeField] private bool buildFloor = true;
    [SerializeField] private bool useExactWeightedFloorDistribution = true;

    [Header("Border")]
    [SerializeField] private bool buildBorder = true;
    [SerializeField, Min(0)] private int doorOpeningTiles = 3;
    [SerializeField] private bool useDefinitionDoorOpeningSize = true;
    [SerializeField] private bool buildDoorFrames = true;

    [Header("Debug")]
    [SerializeField] private bool logBuildResult;

    private readonly List<GameObject> _generatedObjects = new List<GameObject>();
    private Transform _generatedRoot;

    public bool BuildOnRoomInit => buildOnRoomInit;

    public void Build(RoomNode room)
    {
        if (clearBeforeBuild)
            Clear();

        if (!buildOnRoomInit || room == null || room.Definition == null || room.Definition.Skin == null)
            return;

        var skin = room.Definition.Skin;
        var safeTileSize = GetSafeTileSize();
        var columns = Mathf.Max(1, Mathf.RoundToInt(room.Size.x / safeTileSize.x));
        var rows = Mathf.Max(1, Mathf.RoundToInt(room.Size.y / safeTileSize.y));

        CreateGeneratedRoot(room);

        if (buildFloor)
            BuildFloor(room, skin, columns, rows, safeTileSize);

        if (buildBorder)
            BuildBorder(room, skin, columns, rows, safeTileSize);

        if (logBuildResult)
            Debug.Log($"Built tile visuals for room {room.Id} ({room.Type}). Size: {columns}x{rows}, Objects: {_generatedObjects.Count}");
    }

    public void Clear()
    {
        for (var i = 0; i < _generatedObjects.Count; i++)
        {
            if (_generatedObjects[i] != null)
                DestroyGeneratedObject(_generatedObjects[i]);
        }

        _generatedObjects.Clear();

        if (_generatedRoot != null)
        {
            DestroyGeneratedObject(_generatedRoot.gameObject);
            _generatedRoot = null;
        }
    }

    private void CreateGeneratedRoot(RoomNode room)
    {
        var rootObject = new GameObject($"RoomTiles_{room.Id}_{room.Type}");
        _generatedRoot = rootObject.transform;
        _generatedRoot.SetParent(transform, false);
        _generatedRoot.localPosition = Vector3.zero;
        _generatedRoot.localRotation = Quaternion.identity;
        _generatedRoot.localScale = GetInverseParentScale();
    }

    private void BuildFloor(RoomNode room, RoomSkin skin, int columns, int rows, Vector2 safeTileSize)
    {
        var random = CreateRandom(room);
        var tileCount = columns * rows;
        var floorSprites = useExactWeightedFloorDistribution
            ? CreateWeightedFloorSpriteSequence(skin, tileCount, random)
            : null;
        var sequenceIndex = 0;

        for (var x = 0; x < columns; x++)
        {
            for (var y = 0; y < rows; y++)
            {
                var sprite = floorSprites != null && sequenceIndex < floorSprites.Count
                    ? floorSprites[sequenceIndex]
                    : skin.GetRandomFloorSprite(random);

                sequenceIndex++;

                if (sprite == null)
                    continue;

                var localPosition = GetTileLocalPosition(x, y, columns, rows, safeTileSize);
                CreateSpriteObject($"Floor_{x}_{y}", sprite, localPosition, skin.FloorSortingLayerName, skin.FloorOrderInLayer, safeTileSize);
            }
        }
    }


    private List<Sprite> CreateWeightedFloorSpriteSequence(RoomSkin skin, int tileCount, System.Random random)
    {
        if (skin == null || skin.FloorTiles == null || tileCount <= 0)
            return null;

        var validTiles = new List<WeightedSprite>();
        var totalWeight = 0;

        for (var i = 0; i < skin.FloorTiles.Count; i++)
        {
            var tile = skin.FloorTiles[i];
            if (tile == null || tile.Sprite == null || tile.Weight <= 0)
                continue;

            validTiles.Add(tile);
            totalWeight += tile.Weight;
        }

        if (validTiles.Count == 0 || totalWeight <= 0)
            return null;

        var counts = new int[validTiles.Count];
        var remainders = new float[validTiles.Count];
        var assignedCount = 0;

        for (var i = 0; i < validTiles.Count; i++)
        {
            var exactCount = tileCount * (validTiles[i].Weight / (float)totalWeight);
            var count = Mathf.FloorToInt(exactCount);
            counts[i] = count;
            remainders[i] = exactCount - count;
            assignedCount += count;
        }

        while (assignedCount < tileCount)
        {
            var bestIndex = 0;
            var bestRemainder = float.MinValue;

            for (var i = 0; i < remainders.Length; i++)
            {
                if (remainders[i] > bestRemainder)
                {
                    bestRemainder = remainders[i];
                    bestIndex = i;
                }
            }

            counts[bestIndex]++;
            remainders[bestIndex] = -1f;
            assignedCount++;
        }

        var result = new List<Sprite>(tileCount);
        for (var i = 0; i < validTiles.Count; i++)
        {
            for (var count = 0; count < counts[i]; count++)
                result.Add(validTiles[i].Sprite);
        }

        Shuffle(result, random);
        return result;
    }

    private void Shuffle<T>(IList<T> list, System.Random random)
    {
        if (list == null || random == null)
            return;

        for (var i = list.Count - 1; i > 0; i--)
        {
            var swapIndex = random.Next(0, i + 1);
            var temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private void BuildBorder(RoomNode room, RoomSkin skin, int columns, int rows, Vector2 safeTileSize)
    {
        for (var x = 0; x < columns; x++)
        {
            if (!IsDoorOpeningTile(room, Vector2Int.up, x, columns, rows, safeTileSize))
                CreateBorderTile($"Border_Top_{x}", skin.BorderTop, GetTileLocalPosition(x, rows - 1, columns, rows, safeTileSize), skin, safeTileSize);

            if (!IsDoorOpeningTile(room, Vector2Int.down, x, columns, rows, safeTileSize))
                CreateBorderTile($"Border_Bottom_{x}", skin.BorderBottom, GetTileLocalPosition(x, 0, columns, rows, safeTileSize), skin, safeTileSize);
        }

        for (var y = 0; y < rows; y++)
        {
            if (!IsDoorOpeningTile(room, Vector2Int.left, y, columns, rows, safeTileSize))
                CreateBorderTile($"Border_Left_{y}", skin.BorderLeft, GetTileLocalPosition(0, y, columns, rows, safeTileSize), skin, safeTileSize);

            if (!IsDoorOpeningTile(room, Vector2Int.right, y, columns, rows, safeTileSize))
                CreateBorderTile($"Border_Right_{y}", skin.BorderRight, GetTileLocalPosition(columns - 1, y, columns, rows, safeTileSize), skin, safeTileSize);
        }

        CreateBorderTile("Corner_TopLeft", skin.CornerTopLeft, GetTileLocalPosition(0, rows - 1, columns, rows, safeTileSize), skin, safeTileSize);
        CreateBorderTile("Corner_TopRight", skin.CornerTopRight, GetTileLocalPosition(columns - 1, rows - 1, columns, rows, safeTileSize), skin, safeTileSize);
        CreateBorderTile("Corner_BottomLeft", skin.CornerBottomLeft, GetTileLocalPosition(0, 0, columns, rows, safeTileSize), skin, safeTileSize);
        CreateBorderTile("Corner_BottomRight", skin.CornerBottomRight, GetTileLocalPosition(columns - 1, 0, columns, rows, safeTileSize), skin, safeTileSize);

        if (buildDoorFrames)
            BuildDoorFrames(room, skin, columns, rows, safeTileSize);
    }

    private void BuildDoorFrames(RoomNode room, RoomSkin skin, int columns, int rows, Vector2 safeTileSize)
    {
        if (room.Connections == null)
            return;

        for (var i = 0; i < room.Connections.Count; i++)
        {
            var connection = room.Connections[i];
            if (connection == null)
                continue;

            var direction = RoomDataUtility.NormalizeDirection(connection.Direction);
            var doorLocalPosition = room.GetDoorLocalPosition(direction);
            var sprite = direction.x == 0 ? skin.DoorFrameHorizontal : skin.DoorFrameVertical;
            var frameSize = GetDoorFrameTargetSize(room, direction, safeTileSize);

            CreateSpriteObject($"DoorFrame_{DirectionName(direction)}", sprite, doorLocalPosition, skin.DoorFrameSortingLayerName, skin.DoorFrameOrderInLayer, frameSize);
        }
    }

    private void CreateBorderTile(string objectName, Sprite sprite, Vector2 localPosition, RoomSkin skin, Vector2 safeTileSize)
    {
        CreateSpriteObject(objectName, sprite, localPosition, skin.BorderSortingLayerName, skin.BorderOrderInLayer, safeTileSize);
    }

    private void CreateSpriteObject(string objectName, Sprite sprite, Vector2 localPosition, string sortingLayerName, int orderInLayer, Vector2 targetSize)
    {
        if (sprite == null || _generatedRoot == null)
            return;

        var obj = new GameObject(objectName);
        obj.transform.SetParent(_generatedRoot, false);
        obj.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = orderInLayer;

        if (fitSpriteToTileSize && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
        {
            obj.transform.localScale = new Vector3(
                targetSize.x / sprite.bounds.size.x,
                targetSize.y / sprite.bounds.size.y,
                1f);
        }

        _generatedObjects.Add(obj);
    }

    private Vector2 GetTileLocalPosition(int x, int y, int columns, int rows, Vector2 safeTileSize)
    {
        var startX = columns * safeTileSize.x * -0.5f + safeTileSize.x * 0.5f;
        var startY = rows * safeTileSize.y * -0.5f + safeTileSize.y * 0.5f;
        return new Vector2(startX + x * safeTileSize.x, startY + y * safeTileSize.y);
    }

    private bool IsDoorOpeningTile(RoomNode room, Vector2Int direction, int edgeIndex, int columns, int rows, Vector2 safeTileSize)
    {
        if (!HasConnection(room, direction))
            return false;

        var normalized = RoomDataUtility.NormalizeDirection(direction);
        var doorLocalPosition = room.GetDoorLocalPosition(normalized);
        var tileCountOnDoorAxis = normalized.x == 0 ? columns : rows;
        var tileSizeOnDoorAxis = normalized.x == 0 ? safeTileSize.x : safeTileSize.y;
        var doorAxisPosition = normalized.x == 0 ? doorLocalPosition.x : doorLocalPosition.y;
        var openingSize = GetDoorOpeningWorldSize(room, normalized, safeTileSize);

        var axisMin = tileCountOnDoorAxis * tileSizeOnDoorAxis * -0.5f;
        var openingMin = doorAxisPosition - openingSize * 0.5f;
        var openingMax = doorAxisPosition + openingSize * 0.5f;
        var tileMin = axisMin + edgeIndex * tileSizeOnDoorAxis;
        var tileMax = tileMin + tileSizeOnDoorAxis;
        const float epsilon = 0.0001f;

        return tileMin < openingMax - epsilon && tileMax > openingMin + epsilon;
    }



    private float GetDoorOpeningWorldSize(RoomNode room, Vector2Int direction, Vector2 safeTileSize)
    {
        var normalized = RoomDataUtility.NormalizeDirection(direction);
        var tileSizeOnDoorAxis = normalized.x == 0 ? safeTileSize.x : safeTileSize.y;

        if (!useDefinitionDoorOpeningSize || room == null || room.Definition == null)
            return Mathf.Max(1, doorOpeningTiles) * tileSizeOnDoorAxis;

        return Mathf.Max(tileSizeOnDoorAxis, room.Definition.DoorOpeningSize);
    }

    private Vector2 GetDoorFrameTargetSize(RoomNode room, Vector2Int direction, Vector2 safeTileSize)
    {
        var normalized = RoomDataUtility.NormalizeDirection(direction);
        var openingSize = GetDoorOpeningWorldSize(room, normalized, safeTileSize);

        return normalized.x == 0
            ? new Vector2(openingSize, safeTileSize.y)
            : new Vector2(safeTileSize.x, openingSize);
    }

    private bool HasConnection(RoomNode room, Vector2Int direction)
    {
        if (room == null || room.Connections == null)
            return false;

        var normalized = RoomDataUtility.NormalizeDirection(direction);
        for (var i = 0; i < room.Connections.Count; i++)
        {
            var connection = room.Connections[i];
            if (connection != null && RoomDataUtility.NormalizeDirection(connection.Direction) == normalized)
                return true;
        }

        return false;
    }

    private System.Random CreateRandom(RoomNode room)
    {
        var seed = extraRandomSeed;
        if (useRoomIdAsRandomSeed && room != null)
            seed = unchecked(seed * 397 ^ room.Id * 92821 ^ room.GridPos.x * 73856093 ^ room.GridPos.y * 19349663);

        return new System.Random(seed);
    }

    private Vector2 GetSafeTileSize()
    {
        return new Vector2(Mathf.Max(0.01f, tileWorldSize.x), Mathf.Max(0.01f, tileWorldSize.y));
    }

    private string DirectionName(Vector2Int direction)
    {
        var normalized = RoomDataUtility.NormalizeDirection(direction);

        if (normalized == Vector2Int.up) return "Up";
        if (normalized == Vector2Int.down) return "Down";
        if (normalized == Vector2Int.left) return "Left";
        return "Right";
    }


    private Vector3 GetInverseParentScale()
    {
        var scale = transform.lossyScale;
        var x = Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x;
        var y = Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y;
        return new Vector3(x, y, 1f);
    }

    private void DestroyGeneratedObject(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}