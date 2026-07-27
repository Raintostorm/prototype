using System.Collections.Generic;

namespace SpinSquad.Gacha
{
    /// <summary>Hàng đợi ally grant từ roll — tiêu thụ khi <see cref="Core.DuelDirector"/> spawn prep.</summary>
    public static class PendingRollGrants
    {
        static readonly Queue<AllyRollGrant> Queue = new();

        public static void Enqueue(in AllyRollGrant grant) => Queue.Enqueue(grant);

        public static void Clear() => Queue.Clear();

        public static int Count => Queue.Count;

        public static bool TryDequeue(out AllyRollGrant grant) => Queue.TryDequeue(out grant);
    }
}
