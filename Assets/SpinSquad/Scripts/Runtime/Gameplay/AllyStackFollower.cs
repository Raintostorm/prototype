using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Giữ follower đồng bộ vị trí với leader + offset (stack trong ô).</summary>
    public sealed class AllyStackFollower : MonoBehaviour
    {
        Transform _leader;
        Vector3 _offset;

        public void Init(Transform leader, Vector3 offset)
        {
            _leader = leader;
            _offset = offset;
        }

        void LateUpdate()
        {
            if (_leader == null)
                return;
            transform.position = _leader.position + _offset;
        }
    }
}
