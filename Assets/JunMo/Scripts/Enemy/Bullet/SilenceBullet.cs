using UnityEngine;

public class SilenceBullet : BulletBase
{
    [SerializeField] private float silenceDuration = 3f;

    public void Init(float damage, Vector2 direction, float silenceDuration, float speed = 10f)
    {
        this.silenceDuration = silenceDuration;
        base.Init(damage, direction, speed);
    }

    protected override void OnHitPlayer(Collider2D other)
    {
        // other.GetComponent<PlayerSkillController>()?.ApplySilence(silenceDuration);
        // Debug.Log($"[SilenceBullet] Skill silence {silenceDuration} sec");
    }
}
