using UnityEngine;

namespace SpinSquad.Scenes
{
    /// <summary>Phản hồi tối thiểu cho nút HUD stub — sau này có thể thay bằng toast/analytics.</summary>
    public static class StubHudFeedback
    {
        public static void LogComingSoon(string featureId)
        {
            Debug.Log("[SpinSquad][Stub] Coming soon: " + featureId);
        }
    }
}
