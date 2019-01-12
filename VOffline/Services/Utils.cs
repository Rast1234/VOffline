using System;

namespace VOffline.Services
{
    public static class Utils
    {
        public static void ThrowIfCountMismatch(decimal expectedTotal, decimal resultCount)
        {
            if (resultCount != expectedTotal)
            {
                throw new InvalidOperationException($"Expected {expectedTotal} items, got {resultCount}. Maybe they were created/deleted, try again.");
            }
        }
    }
}