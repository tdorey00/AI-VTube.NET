using System.Collections.Concurrent;

namespace AI_Vtube_dotNET.Core.Queues;

/// <summary>
/// A batched queue implementation
/// </summary>
internal class BatchQueue<T>
{
    /// <summary>
    /// How many items will be returned each time the queue is read
    /// </summary>
    public readonly int _batchSize;
    /// <summary>
    /// The maxmimum possible size of the queue
    /// </summary>
    public readonly int _maxSize;

    //TODO: Unsure if concurrent queue is really neccessary, wait until more implementation to find out
    /// <summary>
    /// The queue
    /// </summary>
    private ConcurrentQueue<T> _queue = new();

    /// <summary>
    /// Initialize a <see cref="BatchQueue"/> with a given batch size, max size of the queue defaults to 100
    /// </summary>
    /// <param name="batchSize">The batch size of the queue when items are read</param>
    public BatchQueue(int batchSize)
    {
        _batchSize = batchSize;
        _maxSize = 100;
    }

    /// <summary>
    /// Initialize a <see cref="BatchQueue"/> with a given batch size, and a max size for the queue
    /// </summary>
    /// <param name="batchSize">The batch size of queue when items are read</param>
    /// <param name="maxSize">The maxmimum possible size of the queue</param>
    public BatchQueue(int batchSize, int maxSize)
    {
        _batchSize = batchSize;
        _maxSize = maxSize;
    }

    /// <summary>
    /// Advance the batch by the batch size and return the batch
    /// </summary>
    /// <returns>A <see cref="List{T}"/> that contains the batch that was dequeued</returns>
    public List<T> GetNextBatch()
    {
        List<T> batch = [];
        //Dequeue a batch from the queue
        for (int i = 0; i < _batchSize; i++)
        {
            T value;
            bool success = _queue.TryDequeue(out value!);
            if (!success)
            {
                if (_queue.Count == 0)
                {
                    //If queue is now empty, this is as big of a batch as we can possibly return
                    break;
                }
                else
                {
                    //If for some reason we tried to Dequeue and the queue was not empty, decrement so we can try again to get a full batch
                    i--;
                }
            }
            batch.Add(value);
        }
        return batch;
    }

    /// <summary>
    /// Add a value to the <see cref="BatchQueue{T}"/>
    /// </summary>
    /// <param name="value">The value to add to the <see cref="BatchQueue{T}"/></param>
    /// <returns>True if added, false if the queue has reached the max size</returns>
    public bool Add(T value)
    {
        if (_queue.Count != _maxSize)
        {
            _queue.Enqueue(value);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Advances the queue to the next batch without returning the contained data
    /// </summary>
    public void AdvanceToNextBatch()
    {
        for (int i = 0; i < _batchSize; i++)
        {
            bool success = _queue.TryDequeue(out _);
            if (!success)
            {
                if (_queue.Count == 0)
                {
                    //If queue is now empty, this is as big of a batch as we can possibly discard
                    break;
                }
                else
                {
                    //If for some reason we tried to Dequeue and the queue was not empty, decrement so we can try again to get a full batch
                    i--;
                }
            }
        }
    }
}
