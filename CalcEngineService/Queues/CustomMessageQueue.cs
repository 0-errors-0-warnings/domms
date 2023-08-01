namespace CalcEngineService.Queues;

public class CustomMessageQueue<T> where T: class
{
    private object _syncLock = new();
    private readonly Queue<string> _underliersQueue = new(100000);
    private readonly Dictionary<string, T> _underlierAndMessageDict = new();

    public void Enqueue(string key, T message)
    {
        lock (_syncLock)
        {
            if (!_underlierAndMessageDict.ContainsKey(key))
            {
                // if item does not exists, then add it to the queue
                _underliersQueue.Enqueue(key);
            }

            // now add/update the dict with the latest message
            _underlierAndMessageDict[key] = message;
        }
    }

    public T? Dequeue()
    {
        lock (_syncLock)
        {
            // get the first item from the queue
            if (!_underliersQueue.TryDequeue(out var underlier))
            {
                return null;
            }

            // this should exist in the dict so remove from dict as well
            var psUpdateMessage = _underlierAndMessageDict[underlier];
            _underlierAndMessageDict.Remove(underlier);
            return psUpdateMessage;
        }
    }
}
