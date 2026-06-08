using System.Collections.Generic;
using UnityEngine;

public class RoomBoundaryBuilder : MonoBehaviour
{
    private struct DoorOpening
    {
        public Vector2 Center;
        public Vector2 Size;
        public Vector2Int Direction;
    }

    [Header("Wall")]
    [SerializeField] private bool buildWalls = true;
    [SerializeField, Min(0.01f)] private float wallThickness = 0.5f;
    [SerializeField, Min(0.1f)] private float doorOpeningSize = 2.5f;
    [SerializeField] private string wallLayerName = "Default";

    [Header("Door Lock")]
    [SerializeField, Min(0.01f)] private float doorLockThickness = 1f;
    [SerializeField] private string doorLockLayerName = "Default";

    [Header("Debug")]
    [SerializeField] private bool logBuildResult = true;

    private readonly List<GameObject> _wallObjects = new List<GameObject>();
    private readonly List<GameObject> _doorLockObjects = new List<GameObject>();
    private readonly List<DoorOpening> _doorOpenings = new List<DoorOpening>();

    private Transform _generatedRoot;
    private RoomNode _room;

    public bool HasDoorOpenings => _doorOpenings.Count > 0;

    public void Build(RoomNode room, Transform collisionRoot)
    {
        Clear();

        _room = room;
        if (!buildWalls || _room == null)
            return;

        CreateGeneratedRoot(collisionRoot);
        BuildSide(Vector2Int.up);
        BuildSide(Vector2Int.down);
        BuildSide(Vector2Int.left);
        BuildSide(Vector2Int.right);

        if (logBuildResult)
            Debug.Log($"Built boundary for room {_room.Id} ({_room.Type}). Walls: {_wallObjects.Count}, Door openings: {_doorOpenings.Count}");
    }

    public void BlockDoorOpenings(GameObject blockPrefab)
    {
        if (_doorLockObjects.Count > 0)
            return;

        if (_generatedRoot == null || _doorOpenings.Count == 0)
            return;

        var layer = ResolveLayer(doorLockLayerName);

        for (var i = 0; i < _doorOpenings.Count; i++)
        {
            var opening = _doorOpenings[i];
            var lockSize = GetDoorLockSize(opening);
            GameObject obj;

            if (blockPrefab != null)
            {
                obj = Instantiate(blockPrefab, opening.Center, Quaternion.identity, _generatedRoot);
                obj.name = $"DoorLock_{DirectionName(opening.Direction)}";
                obj.transform.localScale = Vector3.one;
            }
            else
            {
                obj = new GameObject($"DoorLock_{DirectionName(opening.Direction)}");
                obj.transform.SetParent(_generatedRoot, false);
                obj.transform.localPosition = new Vector3(opening.Center.x, opening.Center.y, 0f);
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
            }

            obj.layer = layer;
            var collider = obj.GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = obj.AddComponent<BoxCollider2D>();

            collider.isTrigger = false;
            collider.offset = Vector2.zero;
            collider.size = lockSize;

            _doorLockObjects.Add(obj);
        }
    }

    public void ClearDoorLocks()
    {
        for (var i = 0; i < _doorLockObjects.Count; i++)
        {
            if (_doorLockObjects[i] != null)
                DestroyGeneratedObject(_doorLockObjects[i]);
        }

        _doorLockObjects.Clear();
    }

    public void Clear()
    {
        ClearDoorLocks();

        for (var i = 0; i < _wallObjects.Count; i++)
        {
            if (_wallObjects[i] != null)
                DestroyGeneratedObject(_wallObjects[i]);
        }

        _wallObjects.Clear();
        _doorOpenings.Clear();

        if (_generatedRoot != null)
        {
            DestroyGeneratedObject(_generatedRoot.gameObject);
            _generatedRoot = null;
        }

        _room = null;
    }

    private void CreateGeneratedRoot(Transform collisionRoot)
    {
        var rootObject = new GameObject($"RoomBoundary_{_room.Id}_{_room.Type}");
        _generatedRoot = rootObject.transform;
        _generatedRoot.position = Vector3.zero;
        _generatedRoot.rotation = Quaternion.identity;
        _generatedRoot.localScale = Vector3.one;

        if (collisionRoot != null)
            _generatedRoot.SetParent(collisionRoot, true);
    }

    private void BuildSide(Vector2Int direction)
    {
        var normalized = RoomDataUtility.NormalizeDirection(direction);
        var size = _room.Size;
        var center = _room.Position;
        var halfWidth = size.x * 0.5f;
        var halfHeight = size.y * 0.5f;
        var hasConnection = HasConnection(normalized);

        if (normalized.x != 0)
        {
            var x = center.x + normalized.x * halfWidth;
            var yMin = center.y - halfHeight;
            var yMax = center.y + halfHeight;

            if (!hasConnection)
            {
                CreateWall($"Wall_{DirectionName(normalized)}", new Vector2(x, center.y), new Vector2(wallThickness, size.y));
                return;
            }

            var doorPosition = _room.GetDoorWorldPosition(normalized);
            var opening = Mathf.Min(doorOpeningSize, size.y);
            var halfOpening = opening * 0.5f;
            var doorY = Mathf.Clamp(doorPosition.y, yMin + halfOpening, yMax - halfOpening);
            var openingMin = doorY - halfOpening;
            var openingMax = doorY + halfOpening;

            CreateVerticalSegment(normalized, x, yMin, openingMin, "Bottom");
            CreateVerticalSegment(normalized, x, openingMax, yMax, "Top");
            AddDoorOpening(new Vector2(x, doorY), new Vector2(wallThickness, opening), normalized);
            return;
        }

        var y = center.y + normalized.y * halfHeight;
        var xMin = center.x - halfWidth;
        var xMax = center.x + halfWidth;

        if (!hasConnection)
        {
            CreateWall($"Wall_{DirectionName(normalized)}", new Vector2(center.x, y), new Vector2(size.x, wallThickness));
            return;
        }

        var horizontalDoorPosition = _room.GetDoorWorldPosition(normalized);
        var horizontalOpening = Mathf.Min(doorOpeningSize, size.x);
        var horizontalHalfOpening = horizontalOpening * 0.5f;
        var doorX = Mathf.Clamp(horizontalDoorPosition.x, xMin + horizontalHalfOpening, xMax - horizontalHalfOpening);
        var openingLeft = doorX - horizontalHalfOpening;
        var openingRight = doorX + horizontalHalfOpening;

        CreateHorizontalSegment(normalized, y, xMin, openingLeft, "Left");
        CreateHorizontalSegment(normalized, y, openingRight, xMax, "Right");
        AddDoorOpening(new Vector2(doorX, y), new Vector2(horizontalOpening, wallThickness), normalized);
    }

    private void CreateVerticalSegment(Vector2Int direction, float x, float fromY, float toY, string suffix)
    {
        var length = toY - fromY;
        if (length <= 0.01f)
            return;

        var center = new Vector2(x, (fromY + toY) * 0.5f);
        var size = new Vector2(wallThickness, length);
        CreateWall($"Wall_{DirectionName(direction)}_{suffix}", center, size);
    }

    private void CreateHorizontalSegment(Vector2Int direction, float y, float fromX, float toX, string suffix)
    {
        var length = toX - fromX;
        if (length <= 0.01f)
            return;

        var center = new Vector2((fromX + toX) * 0.5f, y);
        var size = new Vector2(length, wallThickness);
        CreateWall($"Wall_{DirectionName(direction)}_{suffix}", center, size);
    }

    private void CreateWall(string objectName, Vector2 center, Vector2 size)
    {
        var obj = new GameObject(objectName);
        obj.layer = ResolveLayer(wallLayerName);
        obj.transform.SetParent(_generatedRoot, false);
        obj.transform.localPosition = new Vector3(center.x, center.y, 0f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = size;

        _wallObjects.Add(obj);
    }

    private void AddDoorOpening(Vector2 center, Vector2 size, Vector2Int direction)
    {
        _doorOpenings.Add(new DoorOpening
        {
            Center = center,
            Size = size,
            Direction = direction
        });
    }

    private Vector2 GetDoorLockSize(DoorOpening opening)
    {
        var thickness = Mathf.Max(wallThickness, doorLockThickness);
        return opening.Direction.x != 0
            ? new Vector2(thickness, opening.Size.y)
            : new Vector2(opening.Size.x, thickness);
    }

    private bool HasConnection(Vector2Int direction)
    {
        if (_room == null || _room.Connections == null)
            return false;

        var normalized = RoomDataUtility.NormalizeDirection(direction);
        for (var i = 0; i < _room.Connections.Count; i++)
        {
            var connection = _room.Connections[i];
            if (connection != null && RoomDataUtility.NormalizeDirection(connection.Direction) == normalized)
                return true;
        }

        return false;
    }

    private int ResolveLayer(string layerName)
    {
        var layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : 0;
    }

    private string DirectionName(Vector2Int direction)
    {
        var normalized = RoomDataUtility.NormalizeDirection(direction);

        if (normalized == Vector2Int.up) return "Up";
        if (normalized == Vector2Int.down) return "Down";
        if (normalized == Vector2Int.left) return "Left";
        return "Right";
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