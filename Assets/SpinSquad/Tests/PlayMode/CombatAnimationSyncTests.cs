using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpinSquad.Tests.PlayMode
{
    public class CombatAnimationSyncTests
    {
        static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = target.GetType().GetField(fieldName, flags);
            Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        static MonoBehaviour CreateSpriteDriver(float attackDuration, float attackMinInterval, float hitNormalized)
        {
            var go = new GameObject("Test_Line1Driver");
            go.AddComponent<Animator>();
            go.AddComponent<SpriteRenderer>();
            var driverType = System.Type.GetType("SpinSquad.Core.Line1BattleSpriteAnimator, Assembly-CSharp");
            Assert.IsNotNull(driverType, "Could not resolve SpinSquad.Core.Line1BattleSpriteAnimator from Assembly-CSharp.");
            var driver = (MonoBehaviour)go.AddComponent(driverType);
            SetPrivateField(driver, "attackStateDuration", attackDuration);
            SetPrivateField(driver, "attackMinInterval", attackMinInterval);
            SetPrivateField(driver, "attackHitNormalizedTime", hitNormalized);
            return driver;
        }

        static void PlayAttack(MonoBehaviour driver, System.Action onHit, System.Action onComplete)
        {
            var method = driver.GetType().GetMethod("PlayAttack", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(method, "PlayAttack method not found on driver.");
            method.Invoke(driver, new object[] { onHit, onComplete });
        }

        [UnityTest]
        public IEnumerator Line1Attack_FiresExactlyOneHitAndOneComplete()
        {
            var driver = CreateSpriteDriver(attackDuration: 0.10f, attackMinInterval: 0.01f, hitNormalized: 0.3f);
            var hits = 0;
            var completes = 0;

            PlayAttack(driver, () => hits++, () => completes++);
            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(1, hits, "Expected exactly one hit callback per attack.");
            Assert.AreEqual(1, completes, "Expected exactly one complete callback per attack.");

            Object.Destroy(driver.gameObject);
        }

        [UnityTest]
        public IEnumerator Line1Attack_HitOccursBeforeComplete()
        {
            var driver = CreateSpriteDriver(attackDuration: 0.16f, attackMinInterval: 0.01f, hitNormalized: 0.5f);
            var hitTime = -1f;
            var completeTime = -1f;
            var start = Time.time;

            PlayAttack(driver, () => hitTime = Time.time, () => completeTime = Time.time);

            var timeout = Time.time + 1.0f;
            while (completeTime < 0f && Time.time < timeout)
                yield return null;

            Assert.Greater(hitTime, start, "Hit callback should fire after attack starts.");
            Assert.Greater(completeTime, hitTime, "Complete callback should happen after hit callback.");

            Object.Destroy(driver.gameObject);
        }
    }
}
