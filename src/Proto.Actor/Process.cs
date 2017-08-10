// -----------------------------------------------------------------------
//   <copyright file="Process.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using Proto.Mailbox;

namespace Proto
{
    public interface IProcess
    {
        void SendSystemMessage(PID pid, object message);
        void Stop(PID pid);
    }

    public interface IProcess<in T> : IProcess
    {
        void SendUserMessage(PID pid, T message);
    }

    public abstract class Process<T> : IProcess<T>
    {
        public abstract void SendUserMessage(PID pid, T message);

        public virtual void Stop(PID pid)
        {
            SendSystemMessage(pid, new Stop());
        }

        public abstract void SendSystemMessage(PID pid, object message);
    }

    public interface ILocalProcess
    {
        bool IsDead { get; }
    }

    public class LocalProcess<T> : Process<T>, ILocalProcess
    {
        private long _isDead;

        public LocalProcess(IMailbox<T> mailbox)
        {
            Mailbox = mailbox;
        }

        public IMailbox<T> Mailbox { get; }

        public bool IsDead
        {
            get => Interlocked.Read(ref _isDead) == 1;
            private set => Interlocked.Exchange(ref _isDead, value ? 1 : 0);
        }

        public override void SendUserMessage(PID pid, T message)
        {
            Mailbox.PostUserMessage(message);
        }

        public override void SendSystemMessage(PID pid, object message)
        {
            Mailbox.PostSystemMessage(message);
        }

        public override void Stop(PID pid)
        {
            base.Stop(pid);
            IsDead = true;
        }
    }
}