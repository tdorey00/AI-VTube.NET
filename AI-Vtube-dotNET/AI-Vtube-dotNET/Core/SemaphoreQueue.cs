using System.Collections.Concurrent;

namespace AI_Vtube_dotNET.Core
{
    /// <summary>
    /// A Thread Safe FIFO Semaphore
    /// </summary>
    internal class SemaphoreQueue
    {
        private SemaphoreSlim _semaphore;
        private ConcurrentQueue<TaskCompletionSource<bool>> _queue = new();

        /// <summary>
        /// Initializes the <see cref="SemaphoreQueue"/> with the initial number of concurrent requests
        /// </summary>
        /// <param name="initialCount">The initial number of allowed concurrent requests</param>
        public SemaphoreQueue(int initialCount)
        {
            _semaphore = new SemaphoreSlim(initialCount);
        }

        /// <summary>
        /// Initializes the <see cref="SemaphoreQueue"/> with the initial number of concurrent requests and maxmimum requests
        /// </summary>
        /// <param name="initialCount">The initial number of allowed concurrent requests</param>
        /// <param name="maxCount">The maximum number of allowed concurrent requests</param>
        public SemaphoreQueue(int initialCount, int maxCount)
        {
            _semaphore = new SemaphoreSlim(initialCount, maxCount);
        }

        /// <summary>
        /// Wait for the queue to become available
        /// </summary>
        public void Wait()
        {
            WaitAsync().Wait();
        }

        /// <summary>
        /// Returns an waitable task that completes when the queue has availability
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the queue is available</returns>
        public Task WaitAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(tcs);
            _semaphore.WaitAsync().ContinueWith(t =>
            {
                TaskCompletionSource<bool> popped;
                if (_queue.TryDequeue(out popped))
                    popped.SetResult(true);
            });
            return tcs.Task;
        }

        /// <summary>
        /// Release the semaphore and allow the queue to continue processing
        /// </summary>
        public void Release()
        {
            _semaphore.Release();
        }

        public void SetNewCount(int count)
        {
            _semaphore.Wait();
            SemaphoreSlim newSemaphore = new(count);
            newSemaphore.Wait();
            SemaphoreSlim oldSemaphore = _semaphore;
            _semaphore = newSemaphore;
            oldSemaphore.Release();
            _semaphore.Release();
        }
    }
}
