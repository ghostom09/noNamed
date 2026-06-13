using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStatManager statManager;
    [SerializeField] private GameObject        panelRoot;
    [SerializeField] private Transform         rowParent;
    [SerializeField] private PlayerStatRow     rowPrefab;

    [Header("Header UI")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI pointsText;

    [Header("Controls")]
    [SerializeField] private Toggle autoToggle;
    [SerializeField] private Button confirmButton;

    [Header("Options")]
    [SerializeField] private bool pauseTimeOnOpen = true;

    private readonly List<PlayerStatRow> spawnedRows = new();
    private float previousTimeScale = 1f;
    private bool  isAutoMode        = false;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        Hide();
    }

    private void OnEnable()
    {
        if (statManager == null) return;
        statManager.OnPointsChanged += HandlePointsChanged;

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (autoToggle    != null) autoToggle.onValueChanged.AddListener(OnAutoToggle);
    }

    private void OnDisable()
    {
        if (statManager != null)
            statManager.OnPointsChanged -= HandlePointsChanged;

        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
        if (autoToggle    != null) autoToggle.onValueChanged.RemoveListener(OnAutoToggle);
    }

    public void Show()
    {
        if (statManager == null) return;

        BuildRows();
        RefreshHeader();
        RefreshAllRows();

        if (pauseTimeOnOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale    = 0f;
        }

        panelRoot.SetActive(true);
    }

    public void Hide()
    {
        ClearRows();

        if (panelRoot != null) panelRoot.SetActive(false);

        if (pauseTimeOnOpen)
            Time.timeScale = previousTimeScale;
    }

    private void HandlePointsChanged(int remaining)
    {
        RefreshHeader();
        RefreshAllRows();
    }

    private void OnConfirm()
    {
        statManager.ConfirmInvestment();
    }

    private void OnAutoToggle(bool value)
    {
        isAutoMode = value;
        if (isAutoMode) ApplyAutoDistribute();
    }

    private void BuildRows()
    {
        ClearRows();
        if (rowParent == null || rowPrefab == null) return;

        var allTypes = (PlayerStatType[])System.Enum.GetValues(typeof(PlayerStatType));
        foreach (var statType in allTypes)
        {
            PlayerStatRow row = Instantiate(rowPrefab, rowParent);
            row.Init(statType, statManager, OnRowChanged);
            spawnedRows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
            if (row != null) Destroy(row.gameObject);
        spawnedRows.Clear();
    }

    private void OnRowChanged()
    {
        RefreshHeader();
        RefreshAllRows();
    }

    private void RefreshHeader()
    {
        if (statManager == null) return;

        if (levelText  != null) levelText.text  = $"Level: {statManager.Level}";
        if (pointsText != null) pointsText.text = $"Points: {statManager.AvailablePoints}";
    }

    private void RefreshAllRows()
    {
        foreach (var row in spawnedRows)
            row.Refresh();
    }

    private void ApplyAutoDistribute()
    {
        if (statManager == null || statManager.AvailablePoints <= 0) return;

        var allTypes = (PlayerStatType[])System.Enum.GetValues(typeof(PlayerStatType));
        int typeCount = allTypes.Length;
        int remaining = statManager.AvailablePoints;
        int baseAmount = remaining / typeCount;
        int extra      = remaining % typeCount;

        for (int i = 0; i < typeCount; i++)
        {
            int give = baseAmount + (i < extra ? 1 : 0);
            if (give > 0)
                statManager.ChangeInvestment(allTypes[i], give);
        }
    }
}