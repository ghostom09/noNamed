using UnityEngine;

// 플레이어 체력 UI 를 JaeHo 의 PlayerHealth 와 이벤트 기반으로 연결한다.
//
// 주의: FireBoss 팀이 같은 이름의 PlayerHealth(BossSystem.Boss.FireBoss.PlayerHealth)
// 프록시를 PlayerTeamCompatibilityBridge 로 플레이어에 추가한다. 예전엔 타입 "이름"
// 문자열로 탐색해서 FindObjectsByType 순서에 따라 프록시를 잡는 경우가 있었고, 그때는
// 최대 HP 가 fallback(100)으로 뜨고 OnDamaged 이벤트가 없어 UI 가 안 줄었다.
// → 전역 PlayerHealth "타입" 으로만 바인딩해서 프록시와 절대 혼동되지 않게 한다.
[DisallowMultipleComponent]
public class HpBarHealthBinder : MonoBehaviour
{
    [SerializeField] private HpBarView hpBarView;
    [SerializeField] private PlayerHealth health;

    private void Awake()
    {
        BindReferences();
    }

    private void OnEnable()
    {
        BindReferences();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetHealth(PlayerHealth targetHealth)
    {
        if (health == targetHealth)
        {
            return;
        }

        Unsubscribe();
        health = targetHealth;
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (health == null)
        {
            return;
        }

        health.OnDamaged += HandleHpChanged;
        health.OnHealed += HandleHpChanged;
    }

    private void Unsubscribe()
    {
        if (health == null)
        {
            return;
        }

        health.OnDamaged -= HandleHpChanged;
        health.OnHealed -= HandleHpChanged;
    }

    private void HandleHpChanged(float currentHp, float maxHp)
    {
        if (hpBarView != null)
        {
            hpBarView.SetHp(currentHp, maxHp);
        }
    }

    private void Refresh()
    {
        if (hpBarView == null || health == null)
        {
            return;
        }

        hpBarView.SetHp(health.CurrentHp, health.MaxHp);
    }

    private void BindReferences()
    {
        if (hpBarView == null)
        {
            hpBarView = GetComponent<HpBarView>();
        }

        if (hpBarView == null)
        {
            hpBarView = FindAnyObjectByType<HpBarView>();
        }

        if (health == null)
        {
            // 전역 PlayerHealth 타입만 매칭 — FireBoss 프록시(동명 클래스)는 다른 타입이라 제외됨.
            health = FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Exclude);
        }
    }
}
