using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorPoint
{
    [SerializeField] private string id = "Door";
    [SerializeField] private Vector2Int direction = Vector2Int.right;
    [SerializeField] private Vector2 localPosition;

    public string Id => id;
    public Vector2Int Direction => direction;
    public Vector2 LocalPosition => localPosition;
}

[CreateAssetMenu(menuName = "Dungeon/Room Definition")]
public class RoomDefinition : ScriptableObject
{
    [SerializeField] private RoomType type;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Vector2 size = new Vector2(10f, 10f);
    [SerializeField] private List<DoorPoint> doorPoints = new List<DoorPoint>();
    [SerializeField] private bool startsCombat = true;
    [SerializeField] private bool locksDoors = true;
    [SerializeField] private RewardType rewardType = RewardType.None;

    public RoomType Type => type;
    public GameObject Prefab => prefab;
    public Vector2 Size => new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
    public IReadOnlyList<DoorPoint> DoorPoints => doorPoints;
    public bool StartsCombat => startsCombat;
    public bool LocksDoors => locksDoors;
    public RewardType RewardType => rewardType;

    public Vector2 GetDoorLocalPosition(Vector2Int direction)
    {
        var normalized = RoomDataUtility.NormalizeDirection(direction);

        for (var i = 0; i < doorPoints.Count; i++)
        {
            var doorPoint = doorPoints[i];
            if (doorPoint != null && RoomDataUtility.NormalizeDirection(doorPoint.Direction) == normalized)
                return SnapDoorPositionToEdge(doorPoint.LocalPosition, normalized);
        }

        return SnapDoorPositionToEdge(Vector2.zero, normalized);
    }

    private Vector2 SnapDoorPositionToEdge(Vector2 localPosition, Vector2Int direction)
    {
        var safeSize = Size;
        var halfWidth = safeSize.x * 0.5f;
        var halfHeight = safeSize.y * 0.5f;

        if (direction.x != 0)
        {
            var y = Mathf.Clamp(localPosition.y, -halfHeight, halfHeight);
            return new Vector2(direction.x * halfWidth, y);
        }

        var x = Mathf.Clamp(localPosition.x, -halfWidth, halfWidth);
        return new Vector2(x, direction.y * halfHeight);
    }
}
