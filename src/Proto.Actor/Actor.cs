﻿// -----------------------------------------------------------------------
//  <copyright file="Actor.cs" company="Asynkron HB">
//      Copyright (C) 2015-2016 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Proto
{
    public delegate Task Receive(IContext context);

    public class EmptyActor : IActor
    {
        private readonly Receive _receive;

        public EmptyActor(Receive receive)
        {
            _receive = receive;
        }

        public Task ReceiveAsync(IContext context)
        {
            return _receive(context);
        }
    }

    public static class Actor
    {
        public static readonly Task Done = Task.FromResult(0);


        public static EventStream EventStream => EventStream.Instance;

        public static Props FromProducer(Func<IActor> producer)
        {
            return new Props().WithProducer(producer);
        }

        public static Props FromFunc(Receive receive)
        {
            return FromProducer(() => new EmptyActor(receive));
        }

        public static PID Spawn(Props props)
        {
            var name = ProcessRegistry.Instance.NextId();
            return DefaultSpawner(name, props, null);
        }

        public static PID SpawnNamed(Props props, string name)
        {
            return DefaultSpawner(name, props, null);
        }

        public static Spawner DefaultSpawner = (name, props, parent) =>
        {
            var ctx = new Context(props, parent);
            var mailbox = props.MailboxProducer();
            var dispatcher = props.Dispatcher;
            var reff = new LocalActorRef(mailbox);
            var (pid,absent) = ProcessRegistry.Instance.TryAdd(name, reff);
            if (!absent)
            {
                throw new ProcessNameExistException(name);
            }
            ctx.Self = pid;
            mailbox.RegisterHandlers(ctx, dispatcher);

            mailbox.PostSystemMessage(Started.Instance);
            mailbox.Start();

            return pid;
        };
    }

    public class ProcessNameExistException : Exception
    {
        private string _name;

        public ProcessNameExistException(string name)
        {
            _name = name;
        }
    }

    public interface IActor
    {
        Task ReceiveAsync(IContext context);
    }
}