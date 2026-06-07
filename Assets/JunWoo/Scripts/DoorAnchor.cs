using UnityEngine;

public class DoorAnchor : MonoBehaviour
{
    [SerializeField] private Vector2Int direction = Vector2Int.right;

    public Vector2Int Direction => RoomDataUtility.NormalizeDirection(direction);
    public Vector2 LocalPosition => transform.localPosition;
}
