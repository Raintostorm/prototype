using System.Collections;
using Spine.Unity;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Sau khi <see cref="CombatHealth"/> chết: giữ xác, chờ, mờ dần rồi <see cref="Object.Destroy"/>.
    /// </summary>
    public sealed class UnitDeathSequence : MonoBehaviour
    {
        [Tooltip("Giữ corpse bao lâu (sau đó mới bắt đầu mờ).")]
        [SerializeField] float corpseHoldSeconds = 1.2f;

        [Tooltip("Thời gian mờ alpha xuống 0.")]
        [SerializeField] float fadeOutSeconds = 0.45f;

        Coroutine _routine;

        public void Begin()
        {
            if (_routine != null)
                return;

            foreach (var c in GetComponents<Collider2D>())
                c.enabled = false;

            var drag = GetComponent<DuelUnitGridDrag>();
            if (drag != null)
                drag.enabled = false;

            _routine = StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, corpseHoldSeconds));

            var skel = GetComponentInChildren<SkeletonAnimation>(true);
            var sprites = GetComponentsInChildren<SpriteRenderer>(true);

            var spriteBaseA = new float[sprites.Length];
            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    spriteBaseA[i] = sprites[i].color.a;
            }

            var skelStartA = 1f;
            if (skel != null)
                skelStartA = skel.Skeleton.A;

            var dur = Mathf.Max(0.05f, fadeOutSeconds);
            var t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                var k = Mathf.Clamp01(t / dur);
                var mul = 1f - k;
                ApplyAlpha(skel, skelStartA, sprites, spriteBaseA, mul);
                yield return null;
            }

            ApplyAlpha(skel, skelStartA, sprites, spriteBaseA, 0f);
            Destroy(gameObject);
        }

        static void ApplyAlpha(
            SkeletonAnimation skel,
            float skelStartA,
            SpriteRenderer[] sprites,
            float[] spriteBaseA,
            float mul)
        {
            if (skel != null)
                skel.Skeleton.A = skelStartA * mul;

            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null)
                    continue;
                var c = sprites[i].color;
                c.a = spriteBaseA[i] * mul;
                sprites[i].color = c;
            }
        }
    }
}
