using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DelayedEventManager
{
    class TaskManager<T>
    {
        static public long GetCurrentTime()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
        // 忙等待时限控制参数（乘性增 加性减 需要根据使用场景调优）(ms)
        const long InitBusy = 50;
        const long ToleratedDelta = 5;
        const long MinusStep = 1;
        const long MultipleRatio = 2;
        const long MaxBusy = 50;
        const long MinBusy = 5;
        private long _busy = InitBusy;
        // 条件变量
        readonly private object _cv = new object();
        // 优先级队列
        readonly private ConcurrentPriorityQueue<long, T> _queue =
            new ConcurrentPriorityQueue<long, T>((x, y) => (int)(x - y));
        // 对外输出的待处理任务序列
        public BlockingCollection<T> Output { get; } = new BlockingCollection<T>();
        // 停止的flag
        private int _closed = 0;
        public bool Closed { get => _closed == 1; }
        public Task Start()
        {
            return Task.Run(Run);
        }

        private void Run()
        {
            while (!Closed)
            {
                while (_queue.TryPeek(out var pair))
                {
                    long current = GetCurrentTime();
                    if (pair.Key <= current)
                    {
                        var one = _queue.Dequeue();
                        long delta = current - one.Key;
                        if (delta < ToleratedDelta)
                        {
                            _busy -= MinusStep;
                            if (_busy < MinBusy)
                            {
                                _busy = MinBusy;
                            }
                        }
                        else
                        {
                            _busy *= MultipleRatio;
                            if (_busy > MaxBusy)
                            {
                                _busy = MaxBusy;
                            }
                        }
                        Console.WriteLine(_busy);
                        Output.Add(one.Value);
                    }
                    else if (pair.Key <= current + _busy)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                lock (_cv)
                {
                    if (_queue.TryPeek(out var pair))
                    {
                        long dueTime = pair.Key - GetCurrentTime() - _busy;
                        Timer timer = new Timer(o =>
                        {
                            lock (_cv)
                            {
                                Monitor.Pulse(_cv);
                            }
                        }, null, dueTime >= 0 ? dueTime : 0, Timeout.Infinite);
                    }
                    Monitor.Wait(_cv);
                }
            }
        }

        public void Close()
        {
            Interlocked.Exchange(ref _closed, 1);
            // notify the worker to stop by Output
            Output.CompleteAdding();
        }

        public void Add(long delay, T task)
        {
            long key = GetCurrentTime() + delay;
            _queue.Enqueue(key, task);
            lock (_cv)
            {
                Monitor.Pulse(_cv);
            }
        }
    }
}
