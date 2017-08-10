// -----------------------------------------------------------------------
//  <copyright file="UnboundedMailboxQueue.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Proto.Mailbox
{
    public class UnboundedMailboxQueue<T> : IMailboxQueue<T>
    {
        private readonly ConcurrentQueue<T> _messages = new ConcurrentQueue<T>();

        public void Push(T message)
        {
            _messages.Enqueue(message);
        }

        public bool Pop(out T message)
        {
            return _messages.TryDequeue(out message);
        }

        public bool HasMessages => !_messages.IsEmpty;
    }
}