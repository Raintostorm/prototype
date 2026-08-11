#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using SpinSquad.Core;
using SpinSquad.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    /// <summary>Deterministic editor validation for the 2.5D vertical slice.</summary>
    public static class Duel25DVerticalSliceValidator
    {
        const string ScenePath = OpenSampleSceneMenu.Duel25DVerticalSlicePath;

        [MenuItem("SpinSquad/Validate/2.5D Vertical Slice")]
        public static void ValidateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Validate();
            EditorUtility.DisplayDialog(
                "SpinSquad 2.5D",
                "Vertical slice validation passed.",
                "OK");
        }

        /// <summary>Entry point for Unity -executeMethod.</summary>
        public static void ValidateFromCommandLine()
        {
            Validate();
        }

        static void Validate()
        {
            Require(System.IO.File.Exists(ScenePath), $"Missing scene: {ScenePath}");

            var buildScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.path == ScenePath);
            Require(buildScene != null && buildScene.enabled, "2.5D scene is not enabled in Build Settings.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded, "Could not load 2.5D scene.");
            Require(Camera.main != null, "2.5D scene requires a MainCamera.");
            Require(Camera.main.orthographic, "2.5D scene must keep orthographic camera for XY drag accuracy.");
            Require(UnityEngine.Object.FindAnyObjectByType<DuelDirector>() != null,
                "2.5D scene requires DuelDirector.");

            ValidateDamageSignal();
            ValidateUnitPresentation();

            Debug.Log("[SpinSquad][2.5D] Validation passed: scene, build entry, damage signal, shadow, depth scale and sorting.");
        }

        static void ValidateDamageSignal()
        {
            var go = new GameObject("__Validate25D_Health");
            try
            {
                var health = go.AddComponent<CombatHealth>();
                health.Configure(CombatFaction.Enemy, 20f);

                var eventCount = 0;
                var appliedDamage = 0f;
                health.Damaged += (_, amount) =>
                {
                    eventCount++;
                    appliedDamage = amount;
                };

                health.TakeDamage(50f);
                Require(eventCount == 1, "CombatHealth.Damaged should fire once per valid hit.");
                Require(Mathf.Approximately(appliedDamage, 20f),
                    "CombatHealth.Damaged should report HP actually removed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void ValidateUnitPresentation()
        {
            var go = new GameObject("__Validate25D_Unit");
            try
            {
                go.transform.position = new Vector3(0f, -1f, 0f);
                var sprite = go.AddComponent<SpriteRenderer>();
                sprite.sortingOrder = 3;

                var health = go.AddComponent<CombatHealth>();
                health.Configure(CombatFaction.Ally, 30f);
                go.AddComponent<DuelActor>();
                var presentation = go.AddComponent<UnitPresentation25D>();

                var awake = typeof(UnitPresentation25D).GetMethod(
                    "Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var lateUpdate = typeof(UnitPresentation25D).GetMethod(
                    "LateUpdate",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Require(awake != null, "UnitPresentation25D.Awake is missing.");
                Require(lateUpdate != null, "UnitPresentation25D.LateUpdate is missing.");
                awake.Invoke(presentation, null);
                lateUpdate.Invoke(presentation, null);

                Require(go.transform.Find("ContactShadow25D") != null,
                    "UnitPresentation25D did not create a contact shadow.");
                Require(go.transform.localScale.x > 1f,
                    "Front/lower row unit did not receive depth scale.");
                Require(sprite.sortingOrder > 3,
                    "Unit renderer did not enter dynamic 2.5D sorting band.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException($"[SpinSquad][2.5D] {message}");
        }
    }
}
#endif
