using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SpinSquad.Tests.PlayMode
{
    public class BattlePresentation25DTests
    {
        static System.Type ResolveGameType(string fullName)
        {
            var type = System.Type.GetType($"{fullName}, Assembly-CSharp");
            Assert.IsNotNull(type, $"Could not resolve {fullName} from Assembly-CSharp.");
            return type;
        }

        static object Invoke(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Missing method {methodName} on {target.GetType().Name}.");
            return method.Invoke(target, args);
        }

        [Test]
        public void UnitPresentation_AddsShadowDepthScaleAndSorting()
        {
            var go = new GameObject("Test_25DUnit");
            go.transform.position = new Vector3(0f, -1f, 0f);
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 3;

            var healthType = ResolveGameType("SpinSquad.Core.CombatHealth");
            var actorType = ResolveGameType("SpinSquad.Core.DuelActor");
            var presentationType = ResolveGameType("SpinSquad.Presentation.UnitPresentation25D");
            var factionType = ResolveGameType("SpinSquad.Core.CombatFaction");

            var health = go.AddComponent(healthType);
            var allyFaction = System.Enum.Parse(factionType, "Ally");
            Invoke(health, "Configure", allyFaction, 30f);
            go.AddComponent(actorType);
            var presentation = go.AddComponent(presentationType);
            if (go.transform.Find("ContactShadow25D") == null)
                Invoke(presentation, "Awake");
            Invoke(presentation, "LateUpdate");

            var shadow = go.transform.Find("ContactShadow25D");
            Assert.IsNotNull(shadow, "2.5D unit should receive a contact shadow.");
            Assert.Greater(go.transform.localScale.x, 1f, "Lower/front rows should be presented slightly larger.");
            Assert.Greater(spriteRenderer.sortingOrder, 3, "Unit sorting should move into the 2.5D depth band.");

            Object.DestroyImmediate(go);
        }
    }
}
