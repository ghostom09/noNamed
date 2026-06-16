using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class Skill : MonoBehaviour, IPointerClickHandler
{
    private const string HighlightName = "SkillHighlight";
    private const string TooltipPanelName = "SkillTooltipPanel";
    private const string TooltipTitleName = "SkillTooltipTitle";
    private const string TooltipDescriptionName = "SkillTooltipDescription";
    private const string TooltipStatsName = "SkillTooltipStats";

    [SerializeField] private string slotNamePrefix = "SkillImage";
    [SerializeField] private int currentSkillIndex;
    [SerializeField] private Vector2 highlightPadding = new(10f, 10f);
    [SerializeField] private Color highlightColor = new(1f, 0.78f, 0.12f, 0.95f);
    [SerializeField] private bool allowClickSelection = true;
    [SerializeField] private bool allowScrollSelection = true;
    [SerializeField] private bool autoBindRuntimeSkillSwitcher = true;
    [SerializeField] private Vector2 tooltipOffset = new(0f, 120f);
    [SerializeField] private Vector2 tooltipSize = new(280f, 180f);
    [SerializeField] private SkillTooltipData[] skillTooltipData;

    private readonly System.Collections.Generic.List<RectTransform> skillSlots = new();
    private RectTransform highlightRect;
    private Image highlightImage;
    private RectTransform tooltipRect;
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipTitleText;
    private TextMeshProUGUI tooltipDescriptionText;
    private TextMeshProUGUI tooltipStatsText;
    private int hoveredSkillIndex = -1;
    private bool suppressTooltipUntilPointerLeaves;

    public int CurrentSkillIndex => currentSkillIndex;
    public event Action<int> CurrentSkillChanged;

    [System.Serializable]
    public struct SkillTooltipData
    {
        public string skillName;
        [TextArea] public string description;
        [TextArea] public string detailStats;

        public string GetDisplayName(int skillIndex)
        {
            return string.IsNullOrWhiteSpace(skillName) ? $"Skill {skillIndex + 1}" : skillName;
        }
    }

    private void Awake()
    {
        BindSlots();
        BindHighlight();
        BindTooltip();
        RefreshHighlight();
        HideTooltip();
    }

    private void Start()
    {
        EnsureRuntimeSkillBinder();
        RefreshHighlight();
        HideTooltip();
    }

    private void Update()
    {
        HandleScrollInput();
        UpdateTooltipHover();
    }

    private void HandleScrollInput()
    {
        if (!allowScrollSelection)
        {
            return;
        }

        float scrollDelta = GetScrollDelta();

        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        SelectRelativeSkill(scrollDelta > 0f ? -1 : 1);
    }

    private void LateUpdate()
    {
        if (highlightRect != null && highlightRect.gameObject.activeSelf)
        {
            MoveHighlightToCurrentSlot();
        }
    }

    public void SetCurrentSkill(int skillIndex)
    {
        SetCurrentSkillInternal(skillIndex, true);
    }

    public void SetCurrentSkillSilently(int skillIndex)
    {
        SetCurrentSkillInternal(skillIndex, false);
    }

    private void SetCurrentSkillInternal(int skillIndex, bool notify)
    {
        BindSlots();

        if (skillSlots.Count == 0)
        {
            bool changed = currentSkillIndex != 0;
            currentSkillIndex = 0;
            RefreshHighlight();

            if (notify && changed)
            {
                CurrentSkillChanged?.Invoke(currentSkillIndex);
            }

            return;
        }

        int previousIndex = currentSkillIndex;
        currentSkillIndex = Mathf.Clamp(skillIndex, 0, skillSlots.Count - 1);
        RefreshHighlight();

        if (notify && previousIndex != currentSkillIndex)
        {
            CurrentSkillChanged?.Invoke(currentSkillIndex);
        }
    }

    public void SetCurrentSkillNumber(int skillNumber)
    {
        SetCurrentSkill(skillNumber - 1);
    }

    public void ClearCurrentSkill()
    {
        bool changed = currentSkillIndex != -1;
        currentSkillIndex = -1;
        RefreshHighlight();

        if (changed)
        {
            CurrentSkillChanged?.Invoke(currentSkillIndex);
        }
    }

    public void SetSkillTooltipData(int skillIndex, SkillTooltipData tooltipData)
    {
        if (skillIndex < 0)
        {
            return;
        }

        EnsureTooltipDataSize(skillIndex + 1);
        skillTooltipData[skillIndex] = tooltipData;
        RefreshTooltipText();
    }

    public void SetSkillTooltipData(int skillIndex, string skillName, string description, string detailStats)
    {
        SetSkillTooltipData(skillIndex, new SkillTooltipData
        {
            skillName = skillName,
            description = description,
            detailStats = detailStats
        });
    }

    public void SelectRelativeSkill(int delta)
    {
        BindSlots();

        if (skillSlots.Count == 0)
        {
            bool changed = currentSkillIndex != 0;
            currentSkillIndex = 0;
            RefreshHighlight();

            if (changed)
            {
                CurrentSkillChanged?.Invoke(currentSkillIndex);
            }

            return;
        }

        if (currentSkillIndex < 0)
        {
            currentSkillIndex = 0;
        }

        int previousIndex = currentSkillIndex;
        currentSkillIndex = (currentSkillIndex + delta + skillSlots.Count) % skillSlots.Count;
        RefreshHighlight();

        if (previousIndex != currentSkillIndex)
        {
            CurrentSkillChanged?.Invoke(currentSkillIndex);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!allowClickSelection)
        {
            return;
        }

        if (TryGetSlotIndex(eventData.pointerCurrentRaycast.gameObject, out int raycastSkillIndex) ||
            TryGetSlotIndex(eventData.pointerPressRaycast.gameObject, out raycastSkillIndex))
        {
            SetCurrentSkill(raycastSkillIndex);
            CloseTooltipAfterSelection();
            return;
        }

        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(skillSlots[i], eventData.position, eventData.pressEventCamera))
            {
                SetCurrentSkill(i);
                CloseTooltipAfterSelection();
                return;
            }
        }
    }

    private bool TryGetSlotIndex(GameObject target, out int skillIndex)
    {
        skillIndex = -1;

        if (target == null)
        {
            return false;
        }

        Transform current = target.transform;

        while (current != null && current != transform.parent)
        {
            for (int i = 0; i < skillSlots.Count; i++)
            {
                if (current == skillSlots[i])
                {
                    skillIndex = i;
                    return true;
                }
            }

            if (current == transform)
            {
                break;
            }

            current = current.parent;
        }

        return false;
    }

    private float GetScrollDelta()
    {
        return Mouse.current == null ? 0f : Mouse.current.scroll.ReadValue().y;
    }

    private void UpdateTooltipHover()
    {
        BindSlots();
        BindTooltip();

        if (tooltipPanel == null || !TryGetPointerPosition(out Vector2 pointerPosition))
        {
            HideTooltip();
            return;
        }

        int skillIndex = FindSlotIndexAtScreenPoint(pointerPosition);

        if (skillIndex < 0)
        {
            suppressTooltipUntilPointerLeaves = false;
            HideTooltip();
            return;
        }

        if (suppressTooltipUntilPointerLeaves)
        {
            HideTooltip();
            return;
        }

        ShowTooltip(skillIndex);
    }

    private bool TryGetPointerPosition(out Vector2 pointerPosition)
    {
        if (Mouse.current == null)
        {
            pointerPosition = Vector2.zero;
            return false;
        }

        pointerPosition = Mouse.current.position.ReadValue();
        return true;
    }

    private int FindSlotIndexAtScreenPoint(Vector2 screenPosition)
    {
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(skillSlots[i], screenPosition))
            {
                return i;
            }
        }

        return -1;
    }

    private void BindSlots()
    {
        skillSlots.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (!child.name.StartsWith(slotNamePrefix))
            {
                continue;
            }

            if (child.TryGetComponent(out RectTransform slot))
            {
                skillSlots.Add(slot);

                if (child.TryGetComponent(out Image slotImage))
                {
                    slotImage.raycastTarget = true;
                }
            }
        }
    }

    private void BindHighlight()
    {
        if (highlightRect != null && highlightImage != null)
        {
            return;
        }

        Transform highlight = transform.Find(HighlightName);

        if (highlight == null)
        {
            GameObject highlightObject = new(HighlightName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            highlightObject.transform.SetParent(transform, false);
            highlight = highlightObject.transform;
        }

        highlightRect = highlight.GetComponent<RectTransform>();
        highlightImage = highlight.GetComponent<Image>();

        if (highlightImage != null)
        {
            highlightImage.color = highlightColor;
            highlightImage.raycastTarget = false;
        }

        if (highlight.TryGetComponent(out LayoutElement layoutElement))
        {
            layoutElement.ignoreLayout = true;
        }

        highlight.SetAsFirstSibling();
    }

    private void BindTooltip()
    {
        if (tooltipRect != null && tooltipTitleText != null && tooltipDescriptionText != null && tooltipStatsText != null)
        {
            return;
        }

        Transform panel = transform.Find(TooltipPanelName);

        if (panel == null)
        {
            GameObject panelObject = new(TooltipPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            panelObject.transform.SetParent(transform, false);
            panel = panelObject.transform;
        }

        tooltipPanel = panel.gameObject;
        tooltipRect = panel.GetComponent<RectTransform>();

        if (tooltipRect != null)
        {
            tooltipRect.sizeDelta = tooltipSize;
        }

        if (panel.TryGetComponent(out Image panelImage))
        {
            panelImage.raycastTarget = false;
        }

        if (panel.TryGetComponent(out LayoutElement panelLayout))
        {
            panelLayout.ignoreLayout = true;
        }

        tooltipTitleText = FindOrCreateTooltipText(panel, TooltipTitleName);
        tooltipDescriptionText = FindOrCreateTooltipText(panel, TooltipDescriptionName);
        tooltipStatsText = FindOrCreateTooltipText(panel, TooltipStatsName);
    }

    private TextMeshProUGUI FindOrCreateTooltipText(Transform panel, string textName)
    {
        Transform textTransform = panel.Find(textName);

        if (textTransform == null)
        {
            GameObject textObject = new(textName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel, false);
            textTransform = textObject.transform;
        }

        TextMeshProUGUI text = textTransform.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }

    private void ShowTooltip(int skillIndex)
    {
        BindSlots();
        BindTooltip();

        if (tooltipPanel == null || tooltipRect == null || skillIndex < 0 || skillIndex >= skillSlots.Count)
        {
            return;
        }

        hoveredSkillIndex = skillIndex;
        RefreshTooltipText();
        MoveTooltipToSlot(skillSlots[skillIndex]);
        tooltipPanel.SetActive(true);
    }

    private void HideTooltip()
    {
        hoveredSkillIndex = -1;

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private void CloseTooltipAfterSelection()
    {
        suppressTooltipUntilPointerLeaves = true;
        HideTooltip();
    }

    private void RefreshTooltipText()
    {
        if (hoveredSkillIndex < 0 || tooltipTitleText == null || tooltipDescriptionText == null || tooltipStatsText == null)
        {
            return;
        }

        SkillTooltipData data = GetTooltipData(hoveredSkillIndex);
        tooltipTitleText.text = data.GetDisplayName(hoveredSkillIndex);
        tooltipDescriptionText.text = data.description ?? string.Empty;
        tooltipStatsText.text = data.detailStats ?? string.Empty;
    }

    private SkillTooltipData GetTooltipData(int skillIndex)
    {
        if (skillTooltipData == null || skillIndex < 0 || skillIndex >= skillTooltipData.Length)
        {
            return default;
        }

        return skillTooltipData[skillIndex];
    }

    private void EnsureTooltipDataSize(int size)
    {
        if (skillTooltipData == null)
        {
            skillTooltipData = new SkillTooltipData[size];
            return;
        }

        if (skillTooltipData.Length < size)
        {
            System.Array.Resize(ref skillTooltipData, size);
        }
    }

    private void MoveTooltipToSlot(RectTransform slot)
    {
        if (tooltipRect == null || slot == null)
        {
            return;
        }

        tooltipRect.SetAsLastSibling();
        tooltipRect.anchorMin = slot.anchorMin;
        tooltipRect.anchorMax = slot.anchorMax;
        tooltipRect.pivot = new Vector2(0.5f, 0f);
        tooltipRect.anchoredPosition = slot.anchoredPosition + tooltipOffset;
        tooltipRect.sizeDelta = tooltipSize;
    }

    private void RefreshHighlight()
    {
        BindSlots();
        BindHighlight();

        if (highlightRect == null || skillSlots.Count == 0 || currentSkillIndex < 0)
        {
            if (highlightRect != null)
            {
                highlightRect.gameObject.SetActive(false);
            }

            return;
        }

        currentSkillIndex = Mathf.Clamp(currentSkillIndex, 0, skillSlots.Count - 1);
        highlightRect.gameObject.SetActive(true);
        MoveHighlightToCurrentSlot();
    }

    private void MoveHighlightToCurrentSlot()
    {
        if (currentSkillIndex < 0 || currentSkillIndex >= skillSlots.Count)
        {
            return;
        }

        RectTransform barRect = transform as RectTransform;

        if (barRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
        }

        RectTransform currentSlot = skillSlots[currentSkillIndex];
        highlightRect.SetAsFirstSibling();
        highlightRect.anchorMin = currentSlot.anchorMin;
        highlightRect.anchorMax = currentSlot.anchorMax;
        highlightRect.pivot = currentSlot.pivot;
        highlightRect.anchoredPosition = currentSlot.anchoredPosition;
        highlightRect.sizeDelta = currentSlot.rect.size + highlightPadding;
    }

    private void EnsureRuntimeSkillBinder()
    {
        if (!autoBindRuntimeSkillSwitcher || GetComponent<SkillSelectionBinder>() != null)
        {
            return;
        }

        if (SkillMutationLoadoutBinder.FindFirstComponentByTypeName("SkillSwitcher") != null)
        {
            gameObject.AddComponent<SkillSelectionBinder>();
        }
    }

}
