using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Subscribes to player death, shows the game over overlay, and locks gameplay input.
/// </summary>
[DisallowMultipleComponent]
public class GameOverController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameOverUI ui;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private bool autoFindReferences = true;

    [Header("Scene")]
    [Tooltip("Scene loaded by the main menu button.")]
    [SerializeField] private string mainMenuSceneName = "StartScene";

    [Header("Behaviour")]
    [Tooltip("Pause the game with timeScale = 0 when game over starts.")]
    [SerializeField] private bool pauseOnGameOver = true;
    [SerializeField] private bool lockGameplayInput = true;
    [SerializeField] private bool hideCombatUiOnGameOver = true;

    private readonly List<Behaviour> disabledBehaviours = new();
    private bool buttonsBound;
    private bool gameOverTriggered;

    private void Awake()
    {
        BindReferences();
        BindButtonsOnce();
    }

    private void OnEnable()
    {
        BindReferences();
        BindButtonsOnce();
        Subscribe();

        if (playerHealth != null && playerHealth.IsDead)
        {
            HandleDeath();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetPlayerHealth(PlayerHealth targetHealth)
    {
        if (playerHealth == targetHealth)
        {
            return;
        }

        Unsubscribe();
        playerHealth = targetHealth;
        Subscribe();

        if (isActiveAndEnabled && playerHealth != null && playerHealth.IsDead)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (gameOverTriggered)
        {
            return;
        }

        gameOverTriggered = true;
        BindReferences();

        if (hideCombatUiOnGameOver)
        {
            HideCombatUi();
        }

        if (lockGameplayInput)
        {
            LockGameplayInput();
        }

        if (pauseOnGameOver)
        {
            Time.timeScale = 0f;
        }

        if (ui != null)
        {
            ui.Show();
        }
    }

    private void OnRestartClicked()
    {
        ResumeTime();
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    private void OnMainMenuClicked()
    {
        ResumeTime();

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("[GameOverController] mainMenuSceneName is empty.", this);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ResumeTime()
    {
        UnlockGameplayInput();
        gameOverTriggered = false;

        if (pauseOnGameOver)
        {
            Time.timeScale = 1f;
        }
    }

    private void HideCombatUi()
    {
        foreach (MutationSelectUI mutationUi in FindObjectsByType<MutationSelectUI>(FindObjectsInactive.Include))
        {
            mutationUi.Hide();
        }

        foreach (MutationDescriptionUI descriptionUi in FindObjectsByType<MutationDescriptionUI>(FindObjectsInactive.Include))
        {
            descriptionUi.Hide();
        }

        foreach (CharacterStatUpgradeUI statUi in FindObjectsByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include))
        {
            statUi.Hide();
        }
    }

    private void LockGameplayInput()
    {
        foreach (SkillSwitcher switcher in FindObjectsByType<SkillSwitcher>(FindObjectsInactive.Include))
        {
            switcher.ReleaseAttack();
        }

        DisableBehaviours<PlayerSkillInputAdapter>();
        DisableBehaviours<SkillSwitcher>();
        DisableBehaviours<Skill>();
        DisableBehaviours<PlayerInputReader>();
        DisableBehaviours<PlayerInput>();
        DisableBehaviours<MutationPickup>();
        DisableBehaviours<MutationPickupRuntimeGuard>();
        DisableBehaviours<RoomClearMutationRewardSystem>();
    }

    private void UnlockGameplayInput()
    {
        for (int i = disabledBehaviours.Count - 1; i >= 0; i--)
        {
            Behaviour behaviour = disabledBehaviours[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        disabledBehaviours.Clear();
    }

    private void DisableBehaviours<T>() where T : Behaviour
    {
        foreach (T behaviour in FindObjectsByType<T>(FindObjectsInactive.Include))
        {
            DisableBehaviour(behaviour);
        }
    }

    private void DisableBehaviour(Behaviour behaviour)
    {
        if (behaviour == null || !behaviour.enabled || behaviour == this || behaviour == ui)
        {
            return;
        }

        if (disabledBehaviours.Contains(behaviour))
        {
            return;
        }

        disabledBehaviours.Add(behaviour);
        behaviour.enabled = false;
    }

    private void BindButtonsOnce()
    {
        if (buttonsBound || ui == null)
        {
            return;
        }

        ui.BindButtons(OnRestartClicked, OnMainMenuClicked);
        buttonsBound = true;
    }

    private void Subscribe()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandleDeath;
            playerHealth.OnDeath += HandleDeath;
        }
    }

    private void Unsubscribe()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandleDeath;
        }
    }

    private void BindReferences()
    {
        if (ui == null)
        {
            ui = GetComponent<GameOverUI>();
            if (ui == null)
            {
                ui = FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
            }
        }

        if (!autoFindReferences || playerHealth != null)
        {
            return;
        }

        playerHealth = FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
    }
}
