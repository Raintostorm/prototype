using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.UI
{
    /// <summary>
    /// Cầu nối giữa code UI và Animator cho portrait ally.
    /// Controller chỉ load khi <see cref="UnitDefinition.UiAnimatorResourcesPath"/> có giá trị (không còn hardcode Legends).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AllyUiAnimatorBridge : MonoBehaviour
    {
        [Header("Animator source")]
        [SerializeField] Animator animator;
        [SerializeField] string resourcesControllerPath;

        [Header("Animator parameters")]
        [SerializeField] string rarityIntParam = "RarityTier";
        [SerializeField] string isMeleeBoolParam = "IsMelee";
        [SerializeField] string revealTrigger = "Reveal";
        [SerializeField] string attackTrigger = "Attack";
        [SerializeField] string upgradeTrigger = "Upgrade";
        [SerializeField] string idleStateName = "Idle";

        bool _controllerLoadAttempted;

        void Awake()
        {
            EnsureAnimatorReady();
        }

        public void ConfigureForUnit(UnitDefinition def, Rarity rarityTier)
        {
            var path = def != null ? def.UiAnimatorResourcesPath : null;
            if (string.IsNullOrWhiteSpace(path))
                path = null;

            resourcesControllerPath = path ?? string.Empty;
            _controllerLoadAttempted = false;

            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator != null && path == null)
                animator.runtimeAnimatorController = null;

            EnsureAnimatorReady();
            if (animator == null)
                return;

            var isMelee = def == null || !def.RangedAttack;
            SetIntIfExists(rarityIntParam, (int)rarityTier);
            SetBoolIfExists(isMeleeBoolParam, isMelee);
        }

        public void PlayReveal()
        {
            EnsureAnimatorReady();
            SetTriggerIfExists(revealTrigger);
        }

        public void PlayAttack()
        {
            EnsureAnimatorReady();
            SetTriggerIfExists(attackTrigger);
        }

        public void PlayUpgrade()
        {
            EnsureAnimatorReady();
            SetTriggerIfExists(upgradeTrigger);
        }

        public void PlayIdle()
        {
            EnsureAnimatorReady();
            if (animator == null || string.IsNullOrWhiteSpace(idleStateName))
                return;
            if (animator.runtimeAnimatorController == null)
                return;
            animator.Play(idleStateName, 0, 0f);
        }

        void EnsureAnimatorReady()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null)
                return;

            if (string.IsNullOrWhiteSpace(resourcesControllerPath))
                return;

            var path = resourcesControllerPath.Trim();
            if (animator.runtimeAnimatorController != null)
                return;
            if (_controllerLoadAttempted)
                return;

            _controllerLoadAttempted = true;
            var controller = Resources.Load<RuntimeAnimatorController>(path);
            if (controller != null)
                animator.runtimeAnimatorController = controller;
        }

        void SetTriggerIfExists(string triggerName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(triggerName))
                return;
            if (!HasParam(triggerName, AnimatorControllerParameterType.Trigger))
                return;
            animator.SetTrigger(triggerName);
        }

        void SetIntIfExists(string intName, int value)
        {
            if (animator == null || string.IsNullOrWhiteSpace(intName))
                return;
            if (!HasParam(intName, AnimatorControllerParameterType.Int))
                return;
            animator.SetInteger(intName, value);
        }

        void SetBoolIfExists(string boolName, bool value)
        {
            if (animator == null || string.IsNullOrWhiteSpace(boolName))
                return;
            if (!HasParam(boolName, AnimatorControllerParameterType.Bool))
                return;
            animator.SetBool(boolName, value);
        }

        bool HasParam(string name, AnimatorControllerParameterType type)
        {
            if (animator == null)
                return false;

            var p = animator.parameters;
            for (var i = 0; i < p.Length; i++)
            {
                if (p[i].type == type && p[i].name == name)
                    return true;
            }

            return false;
        }
    }
}
