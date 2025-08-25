using System.Runtime.CompilerServices;

namespace DCEventSystem.Internal.Collections;

/// <summary>
/// Min-heap implementation where lower priority values have higher priority
/// </summary>
    internal sealed class MinHeap<T>(int capacity)
        where T : class
    {
        private struct HeapItem
        {
            public T Value;
            public int Priority;
        }

        private HeapItem[] _items = new HeapItem[capacity];

        internal int Count { get; private set; }

        public void Push(T value, int priority)
        {
            if (Count >= _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }

            _items[Count] = new HeapItem { Value = value, Priority = priority };
            BubbleUp(Count);
            Count++;
        }

        public T Pop()
        {
            var value = _items[0].Value;
            Count--;
            _items[0] = _items[Count];
            _items[Count] = default;
            BubbleDown(0);
            return value;
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                var parentIndex = (index - 1) / 2;
                if (_items[index].Priority >= _items[parentIndex].Priority)
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                var smallest = index;
                var left = 2 * index + 1;
                var right = 2 * index + 2;

                if (left < Count && _items[left].Priority < _items[smallest].Priority)
                    smallest = left;

                if (right < Count && _items[right].Priority < _items[smallest].Priority)
                    smallest = right;

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int i, int j)
        {
            (_items[i], _items[j]) = (_items[j], _items[i]);
        }
    }