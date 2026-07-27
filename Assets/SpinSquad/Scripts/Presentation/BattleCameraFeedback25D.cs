using UnityEngine;

namespace SpinSquad.Presentation
{
    /// <summary>Small unscaled camera impulses and guarded hit-stop for the 2.5D slice.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1100)]
    public sealed class BattleCameraFeedback25D : MonoBehaviour
    {
        public static BattleCameraFeedback25D Active { get; private set; }

        [SerializeField] float shakeDuration = 0.11f;
        [SerializeField] float hitStopSeconds = 0.045f;
        [SerializeField] float hitStopScale = 0.06f;

        Vector3 _restLocalPosition;
        float _shakeUntil;
        float _shakeAmplitude;
        float _hitStopUntil;
        float _resumeTimeScale = 1f;
        bool _holdingHitStop;

        public static BattleCameraFeedback25D Ensure(Camera cam)
        {
            if (cam == null)
                return null;

            var feedback = cam.GetComponent<BattleCameraFeedback25D>();
            if (feedback == null)
                feedback = cam.gameObject.AddComponent<BattleCameraFeedback25D>();
            return feedback;
        }

        void Awake()
        {
            Active = this;
            _restLocalPosition = transform.localPosition;
        }

        void OnDestroy()
        {
            if (Active == this)
                Active = null;
            ReleaseHitStop();
        }

        public void PlayImpact(float amplitude, bool strong)
        {
            _shakeAmplitude = Mathf.Max(_shakeAmplitude, Mathf.Max(0f, amplitude));
            _shakeUntil = Mathf.Max(_shakeUntil, Time.unscaledTime + shakeDuration);

            if (!strong)
                return;

            if (!_holdingHitStop)
                _resumeTimeScale = Mathf.Max(0.01f, Time.timeScale);

            _holdingHitStop = true;
            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + hitStopSeconds);
        }

        void LateUpdate()
        {
            ApplyHitStop();
            ApplyShake();
        }

        void ApplyHitStop()
        {
            if (!_holdingHitStop)
                return;

            if (Time.unscaledTime < _hitStopUntil)
            {
                Time.timeScale = hitStopScale;
                return;
            }

            ReleaseHitStop();
        }

        void ReleaseHitStop()
        {
            if (!_holdingHitStop)
                return;

            _holdingHitStop = false;
            Time.timeScale = _resumeTimeScale;
        }

        void ApplyShake()
        {
            if (Time.unscaledTime >= _shakeUntil)
            {
                transform.localPosition = _restLocalPosition;
                _shakeAmplitude = 0f;
                return;
            }

            var remaining = Mathf.InverseLerp(_shakeUntil, _shakeUntil - shakeDuration, Time.unscaledTime);
            var falloff = Mathf.Clamp01(remaining);
            var offset = Random.insideUnitCircle * (_shakeAmplitude * falloff);
            transform.localPosition = _restLocalPosition + new Vector3(offset.x, offset.y, 0f);
        }
    }
}
