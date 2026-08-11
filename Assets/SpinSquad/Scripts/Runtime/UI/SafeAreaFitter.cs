using UnityEngine;

namespace SpinSquad.UI
{
    /// <summary>Fits a RectTransform to the device safe area using normalized canvas anchors.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rectTransform;
        Rect _lastSafeArea;
        Vector2Int _lastScreenSize;

        void OnEnable()
        {
            _rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height)
                Apply();
        }

        public void Apply()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            var safe = Screen.safeArea;
            _rectTransform.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _rectTransform.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            _lastSafeArea = safe;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
