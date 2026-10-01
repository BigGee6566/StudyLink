using System;
using System.Collections.Concurrent;

namespace StudyLink.Services.Security
{
    /// <summary>
    /// Fixed-window, in-memory rate limiter ported from TutorConnect (registration: 5 attempts per
    /// IP per 15 minutes). In-memory is deliberate: it protects a single web server from casual
    /// abuse without a table write per attempt. A web farm would move this to a shared store.
    /// </summary>
    public sealed class RateLimiter
    {
        private readonly int _limit;
        private readonly TimeSpan _window;
        private readonly Func<DateTime> _now;
        private readonly ConcurrentDictionary<string, Window> _windows = new ConcurrentDictionary<string, Window>();

        public RateLimiter(int limit, TimeSpan window, Func<DateTime> now = null)
        {
            _limit = limit;
            _window = window;
            _now = now ?? (() => DateTime.Now);
        }

        public static RateLimiter Registration { get; } = new RateLimiter(5, TimeSpan.FromMinutes(15));
        public static RateLimiter PasswordReset { get; } = new RateLimiter(5, TimeSpan.FromMinutes(15));
        public static RateLimiter Contact { get; } = new RateLimiter(5, TimeSpan.FromMinutes(15));

        /// <summary>Records an attempt; false when the key has used up its window.</summary>
        public bool TryAcquire(string key)
        {
            var now = _now();
            var window = _windows.AddOrUpdate(
                key ?? "unknown",
                _ => new Window(now, 1),
                (_, w) => now - w.Start >= _window ? new Window(now, 1) : new Window(w.Start, w.Count + 1));

            if (_windows.Count > 10000)
                Prune(now);

            return window.Count <= _limit;
        }

        private void Prune(DateTime now)
        {
            foreach (var entry in _windows)
                if (now - entry.Value.Start >= _window)
                    _windows.TryRemove(entry.Key, out _);
        }

        private struct Window
        {
            public Window(DateTime start, int count)
            {
                Start = start;
                Count = count;
            }

            public DateTime Start { get; }
            public int Count { get; }
        }
    }
}
