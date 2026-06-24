/// <summary>
/// 둔화 효과를 받을 수 있는 오브젝트에 구현.
/// 이동 속도 감소 등의 효과를 적용.
///
/// 사용 예시:
///   if (col.TryGetComponent<ISlowable>(out var slowable))
///       slowable.ApplySlow(0.5f, 3f); // 50% 속도로 3초간
/// </summary>
public interface ISlowable
{
    /// <summary>
    /// 둔화 적용.
    /// </summary>
    /// <param name="multiplier">속도 배율 (0~1). 0.5 = 50% 속도</param>
    /// <param name="duration">지속 시간 (초)</param>
    void ApplySlow(float multiplier, float duration);
}