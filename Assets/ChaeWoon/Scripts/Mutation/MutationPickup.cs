using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Unity setup:
// - Add SpriteRenderer, CircleCollider2D, and MutationPickup to the mutation item object.
// - Enable Is Trigger on CircleCollider2D.
// - Set the Player object tag to "Player".
// - Assign a MutationData asset to mutationData in the Inspector.
public class MutationPickup : MonoBehaviour
{
    [SerializeField] private MutationData mutationData;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool isPlayerInside;

    private Collider2D triggerCollider;
    private bool playerTagLookupFailed;

    public bool IsPlayerInside => isPlayerInside;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        RefreshPlayerOverlap();

        if (!isPlayerInside || !IsAcquireKeyPressed())
        {
            return;
        }

        if (mutationData == null)
        {
            Debug.LogWarning("[MutationPickup] MutationData is not assigned.", this);
            return;
        }

        OpenMutationSelectUI();
    }

    private void OpenMutationSelectUI()
    {
        if (mutationData == null)
        {
            Debug.LogWarning("[MutationPickup] MutationData is not assigned.", this);
            return;
        }

        if (MutationDescriptionUI.Instance != null)
        {
            MutationDescriptionUI.Instance.Hide();
        }

        if (MutationSelectUI.Instance == null)
        {
            Debug.LogWarning("[MutationPickup] MutationSelectUI.Instance was not found.", this);
            return;
        }

        MutationSelectUI.Instance.Show(mutationData);
    }

    private bool IsAcquireKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }

    private void RefreshPlayerOverlap()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
        }

        if (triggerCollider == null || !triggerCollider.enabled)
        {
            return;
        }

        Physics2D.SyncTransforms();

        Bounds bounds = triggerCollider.bounds;
        Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        bool hasPlayer = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (IsPlayerCollider(hits[i]))
            {
                hasPlayer = true;
                break;
            }
        }

        SetPlayerInside(hasPlayer);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        SetPlayerInside(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        SetPlayerInside(false);
    }

    private void SetPlayerInside(bool inside)
    {
        if (isPlayerInside == inside)
        {
            return;
        }

        isPlayerInside = inside;

        if (isPlayerInside)
        {
            ShowDescription();
            return;
        }

        HideDescription();
    }

    private void ShowDescription()
    {
        if (mutationData == null)
        {
            Debug.LogWarning("[MutationPickup] MutationData is not assigned.", this);
            return;
        }

        if (MutationDescriptionUI.Instance == null)
        {
            Debug.LogWarning("[MutationPickup] MutationDescriptionUI.Instance was not found.", this);
            return;
        }

        MutationDescriptionUI.Instance.Show(mutationData);
    }

    private void HideDescription()
    {
        if (MutationDescriptionUI.Instance == null)
        {
            Debug.LogWarning("[MutationPickup] MutationDescriptionUI.Instance was not found.", this);
            return;
        }

        MutationDescriptionUI.Instance.Hide();
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null || other == triggerCollider)
        {
            return false;
        }

        if (TryComparePlayerTag(other))
        {
            return true;
        }

        return SkillMutationLoadoutBinder.HasComponentInParentByTypeName(other, "SkillMutationLoadoutBinder") ||
               SkillMutationLoadoutBinder.HasComponentInParentByTypeName(other, "SkillSwitcher") ||
               SkillMutationLoadoutBinder.HasComponentInParentByTypeName(other, "PlayerMovement") ||
               other.GetComponentInParent<MutationTestPlayerController>() != null;
    }

    private bool TryComparePlayerTag(Collider2D other)
    {
        if (playerTagLookupFailed || string.IsNullOrWhiteSpace(playerTag))
        {
            return false;
        }

        try
        {
            return other.CompareTag(playerTag);
        }
        catch (UnityException)
        {
            playerTagLookupFailed = true;
            return false;
        }
    }
}
