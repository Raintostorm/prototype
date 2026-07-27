using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace SpinSquad.Presentation
{
    using Core;

    /// <summary>Adds row depth, dynamic sorting, contact shadow and hit feedback to one unit.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(950)]
    public sealed class UnitPresentation25D : MonoBehaviour
    {
        const int SortingBandBase = 24;
        const float ReferenceY = 0.2f;

        [SerializeField] float scalePerWorldY = 0.055f;
        [SerializeField] float minimumScaleFactor = 0.88f;
        [SerializeField] float maximumScaleFactor = 1.12f;
        [SerializeField] float shadowWidth = 0.46f;
        [SerializeField] float shadowHeight = 0.13f;
        [SerializeField] float shadowYOffset = -0.08f;
        [SerializeField] float flashSeconds = 0.075f;

        readonly List<RendererSortState> _rendererStates = new();
        readonly List<SpriteColorState> _spriteColors = new();

        CombatHealth _health;
        SkeletonAnimation _skeleton;
        Color _skeletonBaseColor = Color.white;
        GameObject _shadow;
        SpriteRenderer _shadowRenderer;
        Vector3 _initialScale;
        Coroutine _flashRoutine;

        struct RendererSortState
        {
            public Renderer Renderer;
            public int Offset;
        }

        struct SpriteColorState
        {
            public SpriteRenderer Renderer;
            public Color Color;
        }

        void Awake()
        {
            _initialScale = transform.localScale;
            _health = GetComponent<CombatHealth>();
            _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
            if (_skeleton != null && _skeleton.Skeleton != null)
            {
                _skeletonBaseColor = new Color(
                    _skeleton.Skeleton.R,
                    _skeleton.Skeleton.G,
                    _skeleton.Skeleton.B,
                    _skeleton.Skeleton.A);
            }

            CaptureRenderers();
            BuildShadow();
        }

        void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
                _health.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }

            RestoreColors();
        }

        void LateUpdate()
        {
            ApplyDepthScale();
            ApplyDynamicSorting();
            UpdateShadow();
        }

        void CaptureRenderers()
        {
            _rendererStates.Clear();
            _spriteColors.Clear();

            var renderers = GetComponentsInChildren<Renderer>(true);
            var minimumOrder = int.MaxValue;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    minimumOrder = Mathf.Min(minimumOrder, renderers[i].sortingOrder);
            }

            if (minimumOrder == int.MaxValue)
                minimumOrder = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                _rendererStates.Add(new RendererSortState
                {
                    Renderer = renderer,
                    Offset = renderer.sortingOrder - minimumOrder
                });

                if (renderer is SpriteRenderer sprite)
                {
                    _spriteColors.Add(new SpriteColorState
                    {
                        Renderer = sprite,
                        Color = sprite.color
                    });
                }
            }
        }

        void BuildShadow()
        {
            _shadow = new GameObject("ContactShadow25D");
            _shadow.transform.SetParent(transform, false);
            _shadow.transform.localPosition = new Vector3(0f, shadowYOffset, 0.02f);
            _shadow.transform.localScale = new Vector3(shadowWidth, shadowHeight, 1f);

            _shadowRenderer = _shadow.AddComponent<SpriteRenderer>();
            _shadowRenderer.sprite = BattleBackdrop25D.SoftOvalSprite;
            _shadowRenderer.color = new Color(0.01f, 0.015f, 0.025f, 0.42f);
        }

        void ApplyDepthScale()
        {
            var depthFactor = 1f + (ReferenceY - transform.position.y) * scalePerWorldY;
            depthFactor = Mathf.Clamp(depthFactor, minimumScaleFactor, maximumScaleFactor);
            transform.localScale = Vector3.Scale(_initialScale, new Vector3(depthFactor, depthFactor, 1f));
        }

        void ApplyDynamicSorting()
        {
            var bandOrder = SortingBandBase + Mathf.RoundToInt(-transform.position.y * 12f);
            for (var i = 0; i < _rendererStates.Count; i++)
            {
                var state = _rendererStates[i];
                if (state.Renderer != null)
                    state.Renderer.sortingOrder = bandOrder + state.Offset;
            }

            if (_shadowRenderer != null)
                _shadowRenderer.sortingOrder = bandOrder - 2;
        }

        void UpdateShadow()
        {
            if (_shadow == null || _health == null)
                return;

            _shadow.SetActive(!_health.IsDead);

            // Parent scale already applies row depth; keep the local shadow footprint stable.
            _shadow.transform.localPosition = new Vector3(0f, shadowYOffset, 0.02f);
        }

        void OnDamaged(CombatHealth target, float amount)
        {
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(HitFlash());

            var severity = target.Max > 0f ? amount / target.Max : 0f;
            BattleCameraFeedback25D.Active?.PlayImpact(
                Mathf.Lerp(0.018f, 0.055f, Mathf.Clamp01(severity * 3f)),
                severity >= 0.22f);
        }

        void OnDied(CombatHealth target)
        {
            BattleCameraFeedback25D.Active?.PlayImpact(0.07f, true);
        }

        IEnumerator HitFlash()
        {
            ApplyFlashColor(new Color(1f, 0.48f, 0.36f, 1f));
            yield return new WaitForSecondsRealtime(flashSeconds);
            RestoreColors();
            _flashRoutine = null;
        }

        void ApplyFlashColor(Color flash)
        {
            for (var i = 0; i < _spriteColors.Count; i++)
            {
                var state = _spriteColors[i];
                if (state.Renderer == null)
                    continue;

                var color = flash;
                color.a = state.Color.a;
                state.Renderer.color = color;
            }

            if (_skeleton != null && _skeleton.Skeleton != null)
            {
                _skeleton.Skeleton.R = flash.r;
                _skeleton.Skeleton.G = flash.g;
                _skeleton.Skeleton.B = flash.b;
            }
        }

        void RestoreColors()
        {
            for (var i = 0; i < _spriteColors.Count; i++)
            {
                var state = _spriteColors[i];
                if (state.Renderer != null)
                    state.Renderer.color = state.Color;
            }

            if (_skeleton != null && _skeleton.Skeleton != null)
            {
                _skeleton.Skeleton.R = _skeletonBaseColor.r;
                _skeleton.Skeleton.G = _skeletonBaseColor.g;
                _skeleton.Skeleton.B = _skeletonBaseColor.b;
                _skeleton.Skeleton.A = _skeletonBaseColor.a;
            }
        }
    }
}
