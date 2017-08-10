// -----------------------------------------------------------------------
//  <copyright file="Queue.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

namespace Proto.Mailbox
{
    public interface IMailboxQueue<T>
    {
        bool HasMessages { get; }
        void Push(T message);
        bool Pop(out T message);
    }
}