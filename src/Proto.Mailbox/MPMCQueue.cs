//// Based on MPMCQueue.NET by Alexandr Nikitin
//// https://github.com/alexandrnikitin/MPMCQueue.NET
////
//// Original license:
////
//// MIT License
////
//// Copyright(c) 2016 Alexandr Nikitin
////
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
////
//// The above copyright notice and this permission notice shall be included in all
//// copies or substantial portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//// SOFTWARE.

//using System;
//using System.Runtime.InteropServices;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Proto.Mailbox
//{
//    [StructLayout(LayoutKind.Explicit, Size = 192, CharSet = CharSet.Ansi)]
//    public class MPMCQueue<T>
//    {
//        [FieldOffset(0)]
//        private readonly Cell<T>[] _buffer;
//        [FieldOffset(8)]
//        private readonly int _bufferMask;
//        [FieldOffset(64)]
//        private int _enqueuePos;
//        [FieldOffset(128)]
//        private int _dequeuePos;

//        public int Count => _enqueuePos - _dequeuePos;

//        public MPMCQueue(int bufferSize)
//        {
//            if (bufferSize < 2) throw new ArgumentException($"{nameof(bufferSize)} should be greater than 2");
//            if ((bufferSize & (bufferSize - 1)) != 0) throw new ArgumentException($"{nameof(bufferSize)} should be a power of 2");

//            _bufferMask = bufferSize - 1;
//            _buffer = new Cell<T>[bufferSize];

//            for (var i = 0; i < bufferSize; i++)
//            {
//                _buffer[i] = new Cell<T>(i, default(T));
//            }

//            _enqueuePos = 0;
//            _dequeuePos = 0;
//        }

//        public bool TryEnqueue(T item)
//        {
//            do
//            {
//                var buffer = _buffer;
//                var pos = _enqueuePos;
//                var index = pos & _bufferMask;
//                var cell = buffer[index];
//                if (cell.Sequence == pos && Interlocked.CompareExchange(ref _enqueuePos, pos + 1, pos) == pos)
//                {
//                    Volatile.Write(ref buffer[index].Element, item);
//                    buffer[index].Sequence = pos + 1;
//                    return true;
//                }

//                if (cell.Sequence < pos)
//                {
//                    return false;
//                }
//            } while (true);
//        }

//        public void Enqueue(T item)
//        {
//            while (true)
//            {
//                if (TryEnqueue(item))
//                    break;
//                Task.Delay(1).Wait(); // Could be Thread.Sleep(1) or Thread.SpinWait() if the assembly is not portable lib.
//            }
//        }

//        public bool TryDequeue(out T result)
//        {
//            do
//            {
//                var buffer = _buffer;
//                var bufferMask = _bufferMask;
//                var pos = _dequeuePos;
//                var index = pos & bufferMask;
//                var cell = buffer[index];
//                if (cell.Sequence == pos + 1 && Interlocked.CompareExchange(ref _dequeuePos, pos + 1, pos) == pos)
//                {
//                    result = Volatile.Read(ref cell.Element);
//                    buffer[index] = new Cell<T>(pos + bufferMask + 1, default(T));
//                    return true;
//                }

//                if (cell.Sequence < pos + 1)
//                {
//                    result = default(T);
//                    return false;
//                }
//            } while (true);
//        }

//        public T Dequeue()
//        {
//            while (true)
//            {
//                T o;
//                if (TryDequeue(out o))
//                    return o;
//            }
//        }

//        [StructLayout(LayoutKind.Explicit, Size = 16, CharSet = CharSet.Ansi)]
//        private struct Cell<T>
//        {
//            [FieldOffset(0)]
//            public int Sequence;
//            [FieldOffset(8)]
//            public T Element;

//            public Cell(int sequence, T element)
//            {
//                Sequence = sequence;
//                Element = element;
//            }
//        }
//    }
//}