using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Số damage bay lên tại vị trí world (2D), tự mờ rồi hủy.</summary>
    public sealed class FloatingDamagePopup : MonoBehaviour
    {
        TextMesh _mesh;
        Vector3 _start;
        float _age;
        const float Lifetime = 0.78f;
        const float RiseSpeed = 1.2f;

        public static void SpawnAt(Vector3 worldPos, float damageTaken, Color color)
        {
            if (damageTaken <= 0f)
                return;

            worldPos.z = 0f;
            worldPos += new Vector3(Random.Range(-0.06f, 0.06f), Random.Range(-0.02f, 0.04f), 0f);

            var go = new GameObject("FloatingDamage");
            go.transform.position = worldPos;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = Mathf.Abs(damageTaken - Mathf.Round(damageTaken)) < 0.01f
                ? $"-{(int)Mathf.Round(damageTaken)}"
                : $"-{damageTaken:F1}";
            mesh.fontSize = 64;
            mesh.characterSize = 0.055f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
                mesh.font = font;

            var mr = mesh.GetComponent<MeshRenderer>();
            mr.sortingOrder = 520;

            var pop = go.AddComponent<FloatingDamagePopup>();
            pop._mesh = mesh;
            pop._start = worldPos;
        }

        void Update()
        {
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / Lifetime);
            transform.position = _start + Vector3.up * (RiseSpeed * _age);
            var c = _mesh.color;
            c.a = 1f - t * t;
            _mesh.color = c;
            if (_age >= Lifetime)
                Destroy(gameObject);
        }
    }
}
