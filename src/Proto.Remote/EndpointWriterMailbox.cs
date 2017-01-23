﻿// -----------------------------------------------------------------------
//  <copyright file="EndpointWriterMailbox.cs" company="Asynkron HB">
//      Copyright (C) 2015-2016 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Proto.Remote
{
    internal static class MailboxStatus
    {
        public const int Idle = 0;
        public const int Busy = 1;
    }

    public class EndpointWriterMailbox : IMailbox
    {
        private readonly IMailboxQueue _systemMessages = new BoundedMailboxQueue(4);
        private readonly IMailboxQueue _userMessages = new UnboundedMailboxQueue();// new BoundedMailboxQueue(1024*1024);
        private IDispatcher _dispatcher;
        private IMessageInvoker _invoker;

        private int _status = MailboxStatus.Idle;
        private bool _suspended;

        public void PostUserMessage(object msg)
        {
            _userMessages.Push(msg);
            Schedule();
        }

        public void PostSystemMessage(SystemMessage sys)
        {
            _systemMessages.Push(sys);
            Schedule();
        }

        public void RegisterHandlers(IMessageInvoker invoker, IDispatcher dispatcher)
        {
            _invoker = invoker;
            _dispatcher = dispatcher;
        }

        private async Task RunAsync()
        {
            var t = _dispatcher.Throughput;
            var batch = new List<MessageEnvelope>();
            var sys = (SystemMessage)_systemMessages.Pop();
            if (sys != null)
            {
                if (sys is SuspendMailbox)
                {
                    _suspended = true;
                }
                if (sys is ResumeMailbox)
                {
                    _suspended = false;
                }
                await _invoker.InvokeSystemMessageAsync(sys);
            }
            if (!_suspended)
            {
                batch.Clear();
                object msg;
                while ((msg = _userMessages.Pop()) != null)
                {
                    batch.Add((MessageEnvelope) msg);
                    if (batch.Count > 1000)
                    {
                        break;
                    }
                }

                if (batch.Count > 0)
                {
                    await _invoker.InvokeUserMessageAsync(batch);
                }
            }


            Interlocked.Exchange(ref _status, MailboxStatus.Idle);

            if (_userMessages.HasMessages || _systemMessages.HasMessages)
            {
                Schedule();
            }
        }

        protected void Schedule()
        {
            if (Interlocked.Exchange(ref _status, MailboxStatus.Busy) == MailboxStatus.Idle)
            {
                _dispatcher.Schedule(RunAsync);
            }
        }
    }
}