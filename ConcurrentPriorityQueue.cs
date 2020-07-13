using System;
using System.Collections.Generic;

namespace DelayedEventManager
{
    /// <summary>
    /// 基于最小堆的优先队列
    /// </summary>
    /// <typeparam name="K">Key的类型</typeparam>
    /// <typeparam name="V">Value的类型</typeparam>
    class ConcurrentPriorityQueue<TKey, TValue>
    {
        public sealed class Pair
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            public Pair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        const int InitCapacity = 20;

        readonly private object _lock = new object();
        readonly private Comparison<TKey> _comparison;
        readonly private List<Pair> _array;

        public ConcurrentPriorityQueue(Comparison<TKey> comparison, int initCapacity = InitCapacity)
        {
            _comparison = comparison;
            _array = new List<Pair>(initCapacity);
        }

        private int LeftChild(int i)
        {
            return 2 * i + 1;
        }
        private int RightChild(int i)
        {
            return 2 * i + 2;
        }
        private int Parent(int i)
        {
            return (i - 1) / 2;
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _array.Count;
                }
            }
        }

        public Pair Peek()
        {
            lock (_lock)
            {
                return _array[0];
            }
        }

        public bool TryPeek(out Pair pair)
        {
            lock (_lock)
            {
                if (Count > 0)
                {
                    pair = Peek();
                    return true;
                }
                else
                {
                    pair = null;
                    return false;
                }
            }
        }

        public void Enqueue(TKey key, TValue value)
        {
            Enqueue(new Pair(key, value));
        }
        public void Enqueue(Pair pair)
        {
            lock (_lock)
            {
                int end = _array.Count;
                _array.Add(pair);
                while (end != 0 && _comparison(pair.Key, _array[Parent(end)].Key) < 0)
                {
                    _array[end] = _array[Parent(end)];
                    end = Parent(end);
                }
                _array[end] = pair;
            }
        }

        public Pair Dequeue()
        {
            Pair pair;
            lock (_lock)
            {
                pair = _array[0];
                int end = _array.Count - 1;
                Pair cur = _array[end];
                _array.RemoveAt(end);
                if (_array.Count > 0)
                {
                    int start = 0;
                    while (true)
                    {
                        int leftChild = LeftChild(start);
                        int rightChild = RightChild(start);
                        int minChild;
                        if (leftChild >= _array.Count)
                            break;
                        if (rightChild >= _array.Count)
                        {
                            minChild = leftChild;
                        }
                        else
                        {
                            minChild = _comparison(_array[leftChild].Key, _array[rightChild].Key) < 0 ?
                                leftChild : rightChild;
                        }
                        if (_comparison(cur.Key, _array[minChild].Key) > 0)
                        {
                            _array[start] = _array[minChild];
                            start = minChild;
                        }
                        else
                        {
                            break;
                        }
                    }
                    _array[start] = cur;
                }
            }
            return pair;
        }

        public bool TryDequeue(out Pair pair)
        {
            lock (_lock)
            {
                if (Count > 0)
                {
                    pair = Dequeue();
                    return true;
                }
                else
                {
                    pair = null;
                    return false;
                }
            }
        }
    }
}
