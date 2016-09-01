using System;
using System.Collections.Generic;
using System.Threading;

public class ConcurrentQueue<T>
{
    private readonly object syncLock = new object();
    private Queue<T> queue;

    public int Count
    {
        get
        {
            lock (syncLock)
            {
                return queue.Count;
            }
        }
    }

    public ConcurrentQueue()
    {
        this.queue = new Queue<T>();
    }

    public T Peek()
    {
        lock (syncLock)
        {
            return queue.Peek();
        }
    }

    public void Enqueue(T obj)
    {
        lock (syncLock)
        {
            queue.Enqueue(obj);
            if( queue.Count <= 1)
            {
                Monitor.Pulse(syncLock);
            }
        }
    }

    public T Dequeue()
    {
        lock (syncLock)
        {
            while (true)
            {
                T result;
                if(queue.Count > 0 )
                {
                    return queue.Dequeue();
                }
                else
                {
                    Monitor.Wait(syncLock);
                }
            }
        }
    }

    public bool TryDequeue(out T result)
    {
        lock (syncLock)
        {
            if (queue.Count > 0)
            {
                result = queue.Dequeue();
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }
    }

    public void Clear()
    {
        lock (syncLock)
        {
            queue.Clear();
        }
    }

    public T[] CopyToArray()
    {
        lock (syncLock)
        {
            if (queue.Count == 0)
            {
                return new T[0];
            }

            T[] values = new T[queue.Count];
            queue.CopyTo(values, 0);
            return values;
        }
    }

    public static ConcurrentQueue<T> InitFromArray(IEnumerable<T> initValues)
    {
        var queue = new ConcurrentQueue<T>();

        if (initValues == null)
        {
            return queue;
        }

        foreach (T val in initValues)
        {
            queue.Enqueue(val);
        }

        return queue;
    }
}
