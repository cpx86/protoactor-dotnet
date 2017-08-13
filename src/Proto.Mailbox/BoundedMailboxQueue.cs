//// -----------------------------------------------------------------------
////  <copyright file="BoundedMailboxQueue.cs" company="Asynkron HB">
////      Copyright (C) 2015-2017 Asynkron HB All rights reserved
////  </copyright>
//// -----------------------------------------------------------------------

//namespace Proto.Mailbox
//{
//    internal class BoundedMailboxQueue<T> : IMailboxQueue<T>
//    {
//        private readonly MPMCQueue<T> _messages;

//        public BoundedMailboxQueue(int size)
//        {
//            _messages = new MPMCQueue<T>(size);
//        }

//        public void Push(T message)
//        {
//            _messages.Enqueue(message);
//        }

//        public bool Pop(out T message)
//        {
//            return _messages.TryDequeue(out message);
//        }

//        public bool HasMessages => _messages.Count > 0;
//    }
//}