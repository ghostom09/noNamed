using UnityEngine;

public class SilenceBullet : BulletBase
{
    [SerializeField] private float silenceDuration = 3f;

    protected override void OnHitPlayer(Collider2D other)
    {
        // other.GetComponent<PlayerSkillController>()?.ApplySilence(silenceDuration); 코드가 어떻게 됨?
        Debug.Log($"[SilenceBullet] 스킬 봉인 {silenceDuration}초");
    }
}