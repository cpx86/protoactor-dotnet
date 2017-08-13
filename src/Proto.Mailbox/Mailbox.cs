// -----------------------------------------------------------------------
//  <copyright file="Mailbox.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proto.Mailbox
{
    internal static class MailboxStatus
    {
        public const int Idle = 0;
        public const int Busy = 1;
    }

    public interface IMailbox<T>
    {
        void PostUserMessage(T msg);
        void PostSystemMessage(object msg);
        void RegisterHandlers(IMessageInvoker<T> invoker, IDispatcher dispatcher);
        void Start();
    }

    public static class BoundedMailbox
    {
        public static IMailbox<T> Create<T>(int size, params IMailboxStatistics[] stats)
        {
            throw new NotImplementedException();
            //return new DefaultMailbox<T>(new UnboundedMailboxQueue(), new BoundedMailboxQueue(size), stats);
        }
    }

    public static class UnboundedMailbox
    {
        public static IMailbox<T> Create<T>(params IMailboxStatistics[] stats)
        {
            return new DefaultMailbox<T>(new UnboundedMailboxQueue<object>(), new UnboundedMailboxQueue<T>(), stats);
        }
    }

    internal class DefaultMailbox<T> : IMailbox<T>
    {
        private readonly IMailboxStatistics[] _stats;
        private readonly IMailboxQueue<object> _systemMessages;
        private readonly IMailboxQueue<T> _userMailbox;
        private IDispatcher _dispatcher;
        private IMessageInvoker<T> _invoker;

        private int _status = MailboxStatus.Idle;
        private long _systemMessageCount;
        private bool _suspended;

        internal int Status => _status;

        public DefaultMailbox(IMailboxQueue<object> systemMessages, IMailboxQueue<T> userMailbox, params IMailboxStatistics[] stats)
        {
            _systemMessages = systemMessages;
            _userMailbox = userMailbox;
            _stats = stats ?? new IMailboxStatistics[0];
        }

        public void PostUserMessage(T msg)
        {
            _userMailbox.Push(msg);
            foreach (var t in _stats)
            {
                t.MessagePosted(msg);
            }
            Schedule();
        }

        public void PostSystemMessage(object msg)
        {
            _systemMessages.Push(msg);
            Interlocked.Increment(ref _systemMessageCount);
            foreach (var t in _stats)
            {
                t.MessagePosted(msg);
            }
            Schedule();
        }

        public void RegisterHandlers(IMessageInvoker<T> invoker, IDispatcher dispatcher)
        {
            _invoker = invoker;
            _dispatcher = dispatcher;
        }

        public void Start()
        {
            foreach (var t in _stats)
            {
                t.MailboxStarted();
            }
        }

        private Task RunAsync()
        {
            var done = ProcessMessages();

            if (!done)
                // mailbox is halted, awaiting completion of a message task, upon which mailbox will be rescheduled
                return Task.FromResult(0);

            Interlocked.Exchange(ref _status, MailboxStatus.Idle);

            if (_systemMessages.HasMessages || !_suspended && _userMailbox.HasMessages)
            {
                Schedule();
            }
            else
            {
                foreach (var t in _stats)
                {
                    t.MailboxEmpty();
                }
            }
            return Task.FromResult(0);
        }

        private bool ProcessMessages()
        {
            for (var i = 0; i < _dispatcher.Throughput; i++)
            {
                if (Interlocked.Read(ref _systemMessageCount) > 0 && _systemMessages.Pop(out var sysMsg))
                {
                    Interlocked.Decrement(ref _systemMessageCount);
                    if (sysMsg is SuspendMailbox)
                    {
                        _suspended = true;
                    }
                    else if (sysMsg is ResumeMailbox)
                    {
                        _suspended = false;
                    }
                    try
                    {
                        var t = _invoker.InvokeSystemMessageAsync(sysMsg);
                        if (t.IsFaulted)
                        {
                            _invoker.EscalateFailure(t.Exception, sysMsg);
                            continue;
                        }
                        if (!t.IsCompleted)
                        {
                            // if task didn't complete immediately, halt processing and reschedule a new run when task completes
                            t.ContinueWith(RescheduleOnTaskComplete, sysMsg);
                            return false;
                        }
                        foreach (var t1 in _stats)
                        {
                            t1.MessageReceived(sysMsg);
                        }
                    }
                    catch (Exception e)
                    {
                        _invoker.EscalateFailure(e, sysMsg);
                    }
                    continue;
                }
                if (_suspended)
                {
                    break;
                }
                if (_userMailbox.Pop(out T usrMsg))
                {
                    try
                    {
                        var t = _invoker.InvokeUserMessageAsync(usrMsg);
                        if (t.IsFaulted)
                        {
                            _invoker.EscalateFailure(t.Exception, usrMsg);
                            continue;
                        }
                        if (!t.IsCompleted)
                        {
                            // if task didn't complete immediately, halt processing and reschedule a new run when task completes
                            t.ContinueWith(RescheduleOnTaskComplete, usrMsg);
                            return false;
                        }
                        foreach (var t1 in _stats)
                        {
                            t1.MessageReceived(usrMsg);
                        }
                    }
                    catch (Exception e)
                    {
                        _invoker.EscalateFailure(e, usrMsg);
                    }
                }
                else
                {
                    break;
                }
            }
            return true;
        }

        private void RescheduleOnTaskComplete(Task task, object message)
        {
            if (task.IsFaulted)
            {
                _invoker.EscalateFailure(task.Exception, message);
            }
            else
            {
                foreach (var t in _stats)
                {
                    t.MessageReceived(message);
                }
            }
            _dispatcher.Schedule(RunAsync);
        }


        protected void Schedule()
        {
            if (Interlocked.CompareExchange(ref _status, MailboxStatus.Busy, MailboxStatus.Idle) == MailboxStatus.Idle)
            {
                _dispatcher.Schedule(RunAsync);
            }
        }
    }

    /// <summary>
    /// Extension point for getting notifications about mailbox events
    /// </summary>
    public interface IMailboxStatistics
    {
        /// <summary>
        /// This method is invoked when the mailbox is started
        /// </summary>
        void MailboxStarted();
        /// <summary>
        /// This method is invoked when a message is posted to the mailbox.
        /// </summary>
        void MessagePosted(object message);
        /// <summary>
        /// This method is invoked when a message has been received by the invoker associated with the mailbox.
        /// </summary>
        void MessageReceived(object message);
        /// <summary>
        /// This method is invoked when all messages in the mailbox have been received.
        /// </summary>
        void MailboxEmpty();
    }
}
