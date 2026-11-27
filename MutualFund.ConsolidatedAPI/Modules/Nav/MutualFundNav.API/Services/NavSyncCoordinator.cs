namespace MutualFundNav.API.Services
{
    /// <summary>
    /// Coordinates startup and scheduled NAV synchronization between
    /// NavDownloadWorker, DailyDigestWorker, and portfolio/dashboard read endpoints
    /// so they never read or dispatch stale pre-sync data.
    /// </summary>
    public class NavSyncCoordinator
    {
        private readonly TaskCompletionSource _startupSyncTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly object _lock = new();
        private TaskCompletionSource? _activeSyncTcs;

        /// <summary>
        /// Marks the startup missed-run check (and any resulting sync) as completed.
        /// Safe to call multiple times.
        /// </summary>
        public void MarkStartupSyncCompleted()
        {
            _startupSyncTcs.TrySetResult();
        }

        /// <summary>
        /// Signals that an active NAV download and snapshot sync has started.
        /// </summary>
        public void BeginActiveSync()
        {
            lock (_lock)
            {
                if (_activeSyncTcs == null || _activeSyncTcs.Task.IsCompleted)
                {
                    _activeSyncTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
        }

        /// <summary>
        /// Signals that the active NAV download and snapshot sync has finished.
        /// </summary>
        public void EndActiveSync()
        {
            lock (_lock)
            {
                _activeSyncTcs?.TrySetResult();
            }
        }

        /// <summary>
        /// Waits for startup sync and any currently active NAV sync to complete,
        /// up to <paramref name="timeout"/>. Returns immediately once idle.
        /// </summary>
        public async Task WaitForSyncAsync(TimeSpan timeout, CancellationToken ct = default)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            try
            {
                if (!_startupSyncTcs.Task.IsCompleted)
                {
                    await _startupSyncTcs.Task.WaitAsync(timeoutCts.Token);
                }

                Task? activeTask;
                lock (_lock)
                {
                    activeTask = _activeSyncTcs?.Task;
                }

                if (activeTask != null && !activeTask.IsCompleted)
                {
                    await activeTask.WaitAsync(timeoutCts.Token);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout reached — allow caller to proceed rather than hanging indefinitely.
            }
        }
    }
}
