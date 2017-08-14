// -----------------------------------------------------------------------
//   <copyright file="Props.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using Proto.Mailbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Proto
{
    //public delegate Task SenderMiddlewareDelegate<T>(ISenderContext<T> ctx, PID target, MessageEnvelope<T> envelope);
    //public interface ISenderMiddleware
    //{
    //    Task Invoke<T>(ISenderContext<T> ctx, PID target, MessageEnvelope<T> envelope, SenderMiddlewareDelegate<T> next);
    //}

    //class FooMiddleware : ISenderMiddleware
    //{
    //    public Task Invoke<T>(ISenderContext<T> ctx, PID target, MessageEnvelope<T> envelope, SenderMiddlewareDelegate<T> next)
    //    {
    //        // do stuff
    //        return next(ctx, target, envelope);
    //    }
    //}

    //class BarMiddleware : ISenderMiddleware
    //{
    //    public Task Invoke<T>(ISenderContext<T> ctx, PID target, MessageEnvelope<T> envelope, SenderMiddlewareDelegate<T> next)
    //    {
    //        // do stuff
    //        return next(ctx, target, envelope);
    //    }
    //}

    //class DefaultMiddleware : ISenderMiddleware
    //{
    //    public static readonly DefaultMiddleware Instance = new DefaultMiddleware();
    //    public Task Invoke<T>(ISenderContext<T> ctx, PID target, MessageEnvelope<T> envelope, SenderMiddlewareDelegate<T> next)
    //    {
    //        ((IProcess<MessageEnvelope<T>>)target.Ref).SendUserMessage(target, envelope);
    //        return Task.FromResult(0);
    //    }
    //}

    //class MiddlewareChain : ISenderMiddleware
    //{
    //    private readonly IEnumerable<ISenderMiddleware> _middlewares;

    //    public MiddlewareChain(IEnumerable<ISenderMiddleware> middlewares)
    //    {
    //        _middlewares = middlewares;
    //    }

    //    public Task Invoke<T>(ISenderContext<T> ctx, PID target, MessageEnvelope<T> envelope, SenderMiddlewareDelegate<T> next)
    //    {
    //        var chain = _middlewares
    //            .Reverse()
    //            .Aggregate((ISenderMiddleware)DefaultMiddleware.Instance, (outer, inner) =>
    //            {
    //                return inner.Invoke(ctx, target, envelope, outer.Invoke);
    //            });

    //    }

    //    internal static Task DefaultSender<TMessage>(ISenderContext<TMessage> context, PID target, MessageEnvelope<TMessage> envelope)
    //    {
    //        ((IProcess<MessageEnvelope<TMessage>>)target.Ref).SendUserMessage(target, envelope);
    //        return Task.FromResult(0);
    //    }
    //}

    //class Test
    //{
    //    public void BuildMiddlewareChain(IEnumerable<ISenderMiddleware> middlewares)
    //    {
    //        var chain = new MiddlewareChain(middlewares);
    //    }
    //}

    public sealed class Props<T>
    {
        private Spawner<T> _spawner;
        public Func<IActor<T>> Producer { get; private set; }
        public Func<IMailbox<IMessageEnvelope<T>>> MailboxProducer { get; private set; } = ProduceDefaultMailbox;
        public ISupervisorStrategy SupervisorStrategy { get; private set; } = Supervision.DefaultStrategy;
        public IDispatcher Dispatcher { get; private set; } = Dispatchers.DefaultDispatcher;
        public IList<Func<Receive<T>, Receive<T>>> ReceiveMiddleware { get; private set; } = new List<Func<Receive<T>, Receive<T>>>();
        public IList<Func<Sender<T>, Sender<T>>> SenderMiddleware { get; private set; } = new List<Func<Sender<T>, Sender<T>>>();
        public Receive<T> ReceiveMiddlewareChain { get; set; }
        public Sender<T> SenderMiddlewareChain { get; set; }

        public Spawner<T> Spawner
        {
            get => _spawner ?? DefaultSpawner;
            private set => _spawner = value;
        }

        private static IMailbox<IMessageEnvelope<T>> ProduceDefaultMailbox() => UnboundedMailbox.Create<IMessageEnvelope<T>>();

        public static PID DefaultSpawner(string name, Props<T> props,PID parent)
        {
            var ctx = new LocalContext<T>(props.Producer, props.SupervisorStrategy, props.ReceiveMiddlewareChain, props.SenderMiddlewareChain, parent);
            var mailbox = props.MailboxProducer();
            var dispatcher = props.Dispatcher;
            var process = new LocalProcess<IMessageEnvelope<T>>(mailbox);
            var (pid, absent) = ProcessRegistry.Instance.TryAdd(name, process);
            if (!absent)
            {
                throw new ProcessNameExistException(name);
            }
            ctx.Self = pid;
            mailbox.RegisterHandlers(ctx, dispatcher);
            mailbox.PostSystemMessage(Started.Instance);
            mailbox.Start();

            return pid;
        }

        public Props<T> WithProducer(Func<IActor<T>> producer) => Copy(props => props.Producer = producer);

        public Props<T> WithDispatcher(IDispatcher dispatcher) => Copy(props => props.Dispatcher = dispatcher);

        public Props<T> WithMailbox(Func<IMailbox<IMessageEnvelope<T>>> mailboxProducer) => Copy(props => props.MailboxProducer = mailboxProducer);

        public Props<T> WithChildSupervisorStrategy(ISupervisorStrategy supervisorStrategy) => Copy(props => props.SupervisorStrategy = supervisorStrategy);

        public Props<T> WithReceiveMiddleware(params Func<Receive<T>, Receive<T>>[] middleware) => Copy(props =>
        {
            //props.ReceiveMiddleware = ReceiveMiddleware.Concat(middleware).ToList();
            //props.ReceiveMiddlewareChain = props.ReceiveMiddleware.Reverse()
            //                                    .Aggregate((Receive<T>) LocalContext<T>.DefaultReceive, (inner, outer) => outer(inner));
        });

        public Props<T> WithSenderMiddleware(params Func<Sender<T>, Sender<T>>[] middleware) => Copy(props =>
        {
            props.SenderMiddleware = SenderMiddleware.Concat(middleware).ToList();
            props.SenderMiddlewareChain = props.SenderMiddleware.Reverse()
                                               .Aggregate((Sender<T>)LocalContext<T>.DefaultSender, (inner, outer) => outer(inner));
        });

        public Props<T> WithSpawner(Spawner<T> spawner) => Copy(props => props.Spawner = spawner);

        private Props<T> Copy(Action<Props<T>> mutator)
        {
            var props = new Props<T>
            {
                Dispatcher = Dispatcher,
                MailboxProducer = MailboxProducer,
                Producer = Producer,
                ReceiveMiddleware = ReceiveMiddleware,
                ReceiveMiddlewareChain = ReceiveMiddlewareChain,
                SenderMiddleware = SenderMiddleware,
                SenderMiddlewareChain = SenderMiddlewareChain,
                Spawner = Spawner,
                SupervisorStrategy = SupervisorStrategy
            };
            mutator(props);
            return props;
        }

        internal PID Spawn(string name, PID parent) => Spawner(name, this, parent);
    }

    public delegate PID Spawner<T>(string id, Props<T> props, PID parent);
}