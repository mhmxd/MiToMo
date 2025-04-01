using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    internal class FixedBuffer<T>
    {
        private readonly T[] _buffer;
        private int _count;
        private int _start; // Points to the oldest element
        private int _end;   // Points to the newest element

        public FixedBuffer(int capacity)
        {
            if (capacity < 1) throw new ArgumentException("Capacity must be greater than 0.");
            _buffer = new T[capacity];
            _count = 0;
            _start = 0;
            _end = -1;
        }

        public void Add(T item)
        {
            _end = (_end + 1) % _buffer.Length; // Move end pointer
            _buffer[_end] = item;

            if (_count < _buffer.Length)
            {
                _count++; // Increase count until we reach capacity
            }
            else
            {
                _start = (_start + 1) % _buffer.Length; // If full, move start pointer
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException("Index out of range.");

                int realIndex = (_start + index) % _buffer.Length;
                return _buffer[realIndex];
            }
        }

        public int Count => _count;

        public T First => _buffer[0];
        public T Last => _buffer[_end];

        public T BeforeLast => _buffer[Count - 1];
    }
}
