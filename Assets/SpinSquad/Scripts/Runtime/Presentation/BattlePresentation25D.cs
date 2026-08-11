using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpinSquad.Presentation
{
    using Core;

    /// <summary>
    /// Scene-scoped 2.5D presentation layer. Gameplay remains on the original XY plane;
    /// this component adds depth cues without changing grid, drag or combat rules.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class BattlePresentation25D : MonoBehaviour
    {
        public const string VerticalSliceSceneName = "Duel25DVerticalSlice";

        [SerializeField, Min(0.05f)] float actorScanInterval = 0.2f;
        [SerializeField] bool createBackdropAccents = true;

        readonly HashSet<int> _knownActors = new();
        float _nextActorScanAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallForVerticalSlice()
        {
            if (SceneManager.GetActiveScene().name != VerticalSliceSceneName)
                return;

            if (FindFirstObjectByType<BattlePresentation25D>() != null)
                return;

            var root = new GameObject("BattlePresentation25D");
            root.AddComponent<BattlePresentation25D>();
        }

        void Awake()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                BattleCameraFeedback25D.Ensure(cam);
                ConfigureCamera(cam);
            }

            if (createBackdropAccents)
                BattleBackdrop25D.Build(transform);
        }

        void Start()
        {
            AttachToNewActors();
        }

        void Update()
        {
            if (Time.unscaledTime < _nextActorScanAt)
                return;

            _nextActorScanAt = Time.unscaledTime + actorScanInterval;
            AttachToNewActors();
            RemoveDestroyedActorIds();
        }

        void AttachToNewActors()
        {
            var actors = FindObjectsByType<DuelActor>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (var i = 0; i < actors.Length; i++)
            {
                var actor = actors[i];
                if (actor == null)
                    continue;

                var id = actor.GetInstanceID();
                if (!_knownActors.Add(id))
                    continue;

                if (actor.GetComponent<UnitPresentation25D>() == null)
                    actor.gameObject.AddComponent<UnitPresentation25D>();
            }
        }

        void RemoveDestroyedActorIds()
        {
            // Actor counts are small. Rebuilding prevents instance IDs from accumulating
            // through repeated arena resets and keeps long Editor sessions predictable.
            var actors = FindObjectsByType<DuelActor>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            _knownActors.Clear();
            for (var i = 0; i < actors.Length; i++)
            {
                if (actors[i] != null)
                    _knownActors.Add(actors[i].GetInstanceID());
            }
        }

        static void ConfigureCamera(Camera cam)
        {
            // Keep orthographic projection so all existing ScreenToWorldPoint drag logic
            // remains exact. Depth is communicated through scale, overlap and shadows.
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.035f, 0.055f, 0.095f, 1f);
        }
    }
}
