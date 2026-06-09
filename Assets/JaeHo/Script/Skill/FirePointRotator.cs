using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FirePoint를 플레이어 주위 원 궤도에서 마우스 방향으로 회전시킴.
/// Player 오브젝트에 부착. FirePoint는 Player의 자식 오브젝트.
///
/// 구조:
///   Player
///   └── FirePoint  (이 스크립트가 회전시키는 대상)
///
/// 인스펙터 설정:
///   - firePoint  : FirePoint Transform 연결
///   - orbitRadius: 플레이어 중심에서 FirePoint까지 거리
/// </summary>
public class FirePointRotator : MonoBehaviour
{
    [Header("--- FirePoint Orbit Settings ---")]
    [SerializeField] private Transform firePoint;
    [Tooltip("플레이어 중심에서 FirePoint까지의 반지름")]
    [SerializeField] private float orbitRadius = 1f;

    private Camera _mainCamera;
    private Vector2 _lookScreenPosition;
    private bool _hasExternalLookInput;

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (firePoint == null)
            Debug.LogError($"[FirePointRotator] firePoint가 할당되지 않음");
    }

    private void Update()
    {
        if (firePoint == null) return;

        RotateFirePoint();
    }

    public void SetLookScreenPosition(Vector2 screenPosition)
    {
        _lookScreenPosition = screenPosition;
        _hasExternalLookInput = true;
    }

    private void RotateFirePoint()
    {
        if (!_hasExternalLookInput)
        {
            if (Mouse.current == null) return;
            _lookScreenPosition = Mouse.current.position.ReadValue();
        }

        // 입력 화면 좌표를 월드 좌표로 변환
        Vector2 mouseScreen = _lookScreenPosition;
        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y,
                Mathf.Abs(_mainCamera.transform.position.z)));
        mouseWorld.z = 0f;

        // 플레이어 → 마우스 방향
        Vector2 direction = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;

        // FirePoint를 플레이어 중심 기준 원 궤도 위에 배치
        firePoint.position = (Vector2)transform.position + direction * orbitRadius;

        // FirePoint가 마우스 방향을 바라보도록 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
