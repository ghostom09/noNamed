using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    private readonly Dictionary<Sprite, Tile> _tileCache = new Dictionary<Sprite, Tile>();

    private Transform _generatedRoot;
    private Grid _grid;
    private Tilemap _floorTilemap;
    private Tilemap _borderTilemap;
    private Tilemap _doorFrameTilemap;

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

        CreateGeneratedRoot(room, skin, columns, rows, safeTileSize);

        if (buildFloor)
            BuildFloor(room, skin, columns, rows, safeTileSize);

        if (buildBorder)
            BuildBorder(room, skin, columns, rows, safeTileSize);

        CompressTilemaps();

        if (logBuildResult)
            Debug.Log($"Built tilemap visuals for room {room.Id} ({room.Type}). Size: {columns}x{rows}, Runtime tiles: {_tileCache.Count}");
    }

    public void Clear()
    {
        if (_floorTilemap != null)
            _floorTilemap.ClearAllTiles();

        if (_borderTilemap != null)
            _borderTilemap.ClearAllTiles();

        if (_doorFrameTilemap != null)
            _doorFrameTilemap.ClearAllTiles();

        if (_generatedRoot != null)
        {
            DestroyGeneratedObject(_generatedRoot.gameObject);
            _generatedRoot = null;
        }

        _grid = null;
        _floorTilemap = null;
        _borderTilemap = null;
        _doorFrameTilemap = null;

        foreach (var pair in _tileCache)
        {
            if (pair.Value != null)
                DestroyGeneratedObject(pair.Value);
        }

        _tileCache.Clear();
    }

    private void CreateGeneratedRoot(RoomNode room, RoomSkin skin, int columns, int rows, Vector2 safeTileSize)
    {
        var rootObject = new GameObject($"RoomTilemaps_{room.Id}_{room.Type}");
        _generatedRoot = rootObject.transform;
        _generatedRoot.SetParent(transform, false);
        _generatedRoot.localPosition = Vector3.zero;
        _generatedRoot.localRotation = Quaternion.identity;
        _generatedRoot.localScale = GetInverseParentScale();

        _grid = rootObject.AddComponent<Grid>();
        _grid.cellSize = new Vector3(safeTileSize.x, safeTileSize.y, 1f);
        _grid.cellGap = Vector3.zero;
        _grid.cellLayout = GridLayout.CellLayout.Rectangle;
        _grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        var origin = new Vector3(columns * safeTileSize.x * -0.5f, rows * safeTileSize.y * -0.5f, 0f);

        _floorTilemap = CreateTilemap("FloorTilemap", origin, skin.FloorSortingLayerName, skin.FloorOrderInLayer);
        _borderTilemap = CreateTilemap("BorderTilemap", origin, skin.BorderSortingLayerName, skin.BorderOrderInLayer);
        _doorFrameTilemap = CreateTilemap("DoorFrameTilemap", origin, skin.DoorFrameSortingLayerName, skin.DoorFrameOrderInLayer);
    }

    private Tilemap CreateTilemap(string objectName, Vector3 localPosition, string sortingLayerName, int orderInLayer)
    {
        var obj = new GameObject(objectName);
        obj.transform.SetParent(_generatedRoot, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var tilemap = obj.AddComponent<Tilemap>();
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

        var renderer = obj.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = orderInLayer;

        return tilemap;
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
                SetTile(_floorTilemap, x, y, sprite, safeTileSize);
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
                SetTile(_borderTilemap, x, rows - 1, skin.BorderTop, safeTileSize);

            if (!IsDoorOpeningTile(room, Vector2Int.down, x, columns, rows, safeTileSize))
                SetTile(_borderTilemap, x, 0, skin.BorderBottom, safeTileSize);
        }

        for (var y = 0; y < rows; y++)
        {
            if (!IsDoorOpeningTile(room, Vector2Int.left, y, columns, rows, safeTileSize))
                SetTile(_borderTilemap, 0, y, skin.BorderLeft, safeTileSize);

            if (!IsDoorOpeningTile(room, Vector2Int.right, y, columns, rows, safeTileSize))
                SetTile(_borderTilemap, columns - 1, y, skin.BorderRight, safeTileSize);
        }

        SetTile(_borderTilemap, 0, rows - 1, skin.CornerTopLeft, safeTileSize);
        SetTile(_borderTilemap, columns - 1, rows - 1, skin.CornerTopRight, safeTileSize);
        SetTile(_borderTilemap, 0, 0, skin.CornerBottomLeft, safeTileSize);
        SetTile(_borderTilemap, columns - 1, 0, skin.CornerBottomRight, safeTileSize);

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
            var sprite = direction.x == 0 ? skin.DoorFrameHorizontal : skin.DoorFrameVertical;

            if (direction.x == 0)
            {
                var y = direction.y > 0 ? rows - 1 : 0;
                for (var x = 0; x < columns; x++)
                {
                    if (IsDoorOpeningTile(room, direction, x, columns, rows, safeTileSize))
                        SetTile(_doorFrameTilemap, x, y, sprite, safeTileSize);
                }

                continue;
            }

            var edgeX = direction.x > 0 ? columns - 1 : 0;
            for (var y = 0; y < rows; y++)
            {
                if (IsDoorOpeningTile(room, direction, y, columns, rows, safeTileSize))
                    SetTile(_doorFrameTilemap, edgeX, y, sprite, safeTileSize);
            }
        }
    }

    private void SetTile(Tilemap tilemap, int x, int y, Sprite sprite, Vector2 targetSize)
    {
        if (tilemap == null || sprite == null)
            return;

        tilemap.SetTile(new Vector3Int(x, y, 0), GetTile(sprite, targetSize));
    }

    private Tile GetTile(Sprite sprite, Vector2 targetSize)
    {
        if (sprite == null)
            return null;

        if (_tileCache.TryGetValue(sprite, out var cachedTile) && cachedTile != null)
            return cachedTile;

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = $"RuntimeTile_{sprite.name}";
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;

        if (fitSpriteToTileSize && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
        {
            var scale = new Vector3(
                targetSize.x / sprite.bounds.size.x,
                targetSize.y / sprite.bounds.size.y,
                1f);
            tile.transform = Matrix4x4.Scale(scale);
        }

        _tileCache.Add(sprite, tile);
        return tile;
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

    private void CompressTilemaps()
    {
        if (_floorTilemap != null)
            _floorTilemap.CompressBounds();

        if (_borderTilemap != null)
            _borderTilemap.CompressBounds();

        if (_doorFrameTilemap != null)
            _doorFrameTilemap.CompressBounds();
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