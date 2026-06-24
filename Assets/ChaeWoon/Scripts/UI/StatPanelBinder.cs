using UnityEngine;

[DisallowMultipleComponent]
public class StatPanelBinder : MonoBehaviour
{
    [SerializeField] private bool autoFindHpBarView = true;
    [SerializeField] private bool autoFindPlayerStatManager = true;
    [SerializeField, Min(0.1f)] private float searchInterval = 0.5f;

    private HpBarView hpBarView;
    private PlayerStatManager statManager;
    private PlayerStatManager subscribedManager;
    private float nextSearchTime;

    private void Awake()
    {
        BindReferences(true);
    }

    private void OnEnable()
    {
        BindReferences(true);
        Subscribe();
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextSearchTime)
        {
            return;
        }

        nextSearchTime = Time.unscaledTime + searchInterval;
        BindReferences(false);
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetPlayerStatManager(PlayerStatManager target)
    {
        if (statManager == target)
        {
            return;
        }

        statManager = target;
        Subscribe();
        Refresh();
    }

    private void Refresh()
    {
        if (hpBarView == null || statManager == null)
            return;
        hpBarView.RefreshFromManager(statManager);
    }

    private void BindReferences(bool force)
    {
        if ((force || hpBarView == null) && autoFindHpBarView)
        {
            hpBarView = GetComponent<HpBarView>();
            if (hpBarView == null)
            {
                hpBarView = FindAnyObjectByType<HpBarView>();
            }
        }

        if ((force || statManager == null) && autoFindPlayerStatManager)
        {
            statManager = FindAnyObjectByType<PlayerStatManager>();
        }
    }

    private void Subscribe()
    {
        if (subscribedManager == statManager)
        {
            return;
        }

        Unsubscribe();
        subscribedManager = statManager;

        if (subscribedManager == null)
        {
            return;
        }

        subscribedManager.OnStatsChanged += Refresh;
        subscribedManager.OnPointsChanged += HandlePointsChanged;
    }

    private void Unsubscribe()
    {
        if (subscribedManager == null)
        {
            return;
        }

        subscribedManager.OnStatsChanged -= Refresh;
        subscribedManager.OnPointsChanged -= HandlePointsChanged;
        subscribedManager = null;
    }

    private void HandlePointsChanged(int remainingPoints)
    {
        Refresh();
    }
}
