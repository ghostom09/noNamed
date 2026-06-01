/// <summary>
/// 독 효과를 받을 수 있는 오브젝트에 구현.
/// 일정 시간 동안 DoT(Damage over Time) 데미지 적용.
///
/// 사용 예시:
///   if (col.TryGetComponent<IPoisonable>(out var poisonable))
///       poisonable.ApplyPoison(5f, 3f, 1f); // 초당 5 데미지, 3초간, 1초 간격
/// </summary>
public interface IPoisonable
{
    /// <summary>
    /// 독 적용.
    /// </summary>
    /// <param name="damagePerTick">틱당 데미지</param>
    /// <param name="duration">지속 시간 (초)</param>
    /// <param name="tickInterval">틱 간격 (초)</param>
    void ApplyPoison(float damagePerTick, float duration, float tickInterval);
}