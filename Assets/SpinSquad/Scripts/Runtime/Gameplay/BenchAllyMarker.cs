using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Unit đang nằm trong inventory bench — không tham gia combat cho đến khi deploy lên lưới.</summary>
    public sealed class BenchAllyMarker : MonoBehaviour
    {
        public int SlotIndex { get; internal set; }
    }
}
