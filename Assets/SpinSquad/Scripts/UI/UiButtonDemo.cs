using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>
    /// Ví dụ gắn logic nút: (1) kéo Button vào field trong Inspector,
    /// hoặc (2) để trống field và dùng OnClick trong Inspector trỏ tới public method.
    /// </summary>
    public sealed class UiButtonDemo : MonoBehaviour
    {
        [SerializeField] private Button spinButton;

        private void Awake()
        {
            if (spinButton != null)
                spinButton.onClick.AddListener(OnSpinClicked);
        }

        private void OnDestroy()
        {
            if (spinButton != null)
                spinButton.onClick.RemoveListener(OnSpinClicked);
        }

        public void OnSpinClicked()
        {
            Debug.Log("[SpinSquad] Spin");
        }

        public void OnSettingsClicked()
        {
            Debug.Log("[SpinSquad] Settings");
        }
    }
}
