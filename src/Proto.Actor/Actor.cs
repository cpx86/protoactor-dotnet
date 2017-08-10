// -----------------------------------------------------------------------
//   <copyright file="Actor.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Proto
{
    public delegate Task Receive<T>(IContext<T> context);

    public delegate Task Sender<T>(ISenderContext<T> ctx, PID target, MessageEnvelope envelope);

    public interface IActor<T>
    {
        Task ReceiveAsync(IContext<T> context);
    }

    class EmptyActor<T> : IActor<T>
    {
        private readonly Receive<T> _receive;
        public EmptyActor(Receive<T> receive) => _receive = receive;
        public Task ReceiveAsync(IContext<T> context) => _receive(context);
    }

    public static class Actor
    {
        public static readonly Task Done = Task.FromResult(0);
        public static EventStream EventStream => EventStream.Instance;
        public static Props<T> FromProducer<T>(Func<IActor<T>> producer) => new Props<T>().WithProducer(producer);
        public static Props<T> FromFunc<T>(Receive<T> receive) => FromProducer(() => new EmptyActor<T>(receive));

        public static PID Spawn<T>(Props<T> props)
        {
            var name = ProcessRegistry.Instance.NextId();
            return SpawnNamed(props, name);
        }

        public static PID SpawnPrefix<T>(Props<T> props, string prefix)
        {
            var name = prefix + ProcessRegistry.Instance.NextId();
            return SpawnNamed(props, name);
        }

        public static PID SpawnNamed<T>(Props<T> props, string name)
        {
            return props.Spawn(name, null);
        }
    }

    public class ProcessNameExistException : Exception
    {
        public ProcessNameExistException(string name) : base($"a Process with the name '{name}' already exists")
        {
        }
    }
}