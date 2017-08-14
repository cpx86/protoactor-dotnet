// -----------------------------------------------------------------------
//   <copyright file="LocalContext.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proto.Mailbox;

namespace Proto
{
    internal enum ContextState
    {
        None,
        Alive,
        Restarting,
        Stopping
    }

    public class LocalContext<T> : IMessageInvoker<IMessageEnvelope<T>>, IContext<T>, ISupervisor
    {
        public static readonly IReadOnlyCollection<PID> EmptyChildren = new List<PID>();
        private readonly Func<IActor<T>> _producer;

        private readonly Receive<T> _receiveMiddleware;
        private readonly Sender<T> _senderMiddleware;
        private readonly ISupervisorStrategy _supervisorStrategy;
        private FastSet<PID> _children;
        private IMessageEnvelope<T> _messageEnvelope;

        //TODO: I would like to extract these two as optional components in the future
        //for ReceiveTimeout we could have an object with the SetReceiveTimeout
        //and simply let this object subscribe to actor messages so it knows when to reset the timer
        private Timer _receiveTimeoutTimer;

        private RestartStatistics _restartStatistics;

        //for Stashing, there could be an object with the Stash, Unstash and UnstashAll
        //the main concern for this would be how to make the stash survive between actor restarts
        //if it is injected as a dependency, that would work fine
        private Stack<object> _stash;

        private ContextState _state;
        private FastSet<PID> _watchers;

        public LocalContext(Func<IActor<T>> producer, ISupervisorStrategy supervisorStrategy, Receive<T> receiveMiddleware, Sender<T> senderMiddleware, PID parent)
        {
            _producer = producer;
            _supervisorStrategy = supervisorStrategy;
            _receiveMiddleware = receiveMiddleware;
            _senderMiddleware = senderMiddleware;

            //Parents are implicitly watching the child
            //The parent is not part of the Watchers set
            Parent = parent;

            IncarnateActor();
        }

        private static ILogger Logger { get; } = Log.CreateLogger<LocalContext<T>>();

        public IReadOnlyCollection<PID> Children => _children?.ToList() ?? EmptyChildren;

        public IActor<T> Actor { get; private set; }
        public PID Parent { get; }
        public PID Self { get; set; }

        public T Message
        {
            get => _messageEnvelope.Message;
        }

        public PID Sender => _messageEnvelope.Sender;

        public MessageHeader Headers
        {
            get
            {
                if (_messageEnvelope.Header != null)
                {
                    return _messageEnvelope.Header;
                }
                return MessageHeader.EmptyHeader;
            }
        }

        public TimeSpan ReceiveTimeout { get; private set; }

        public void Stash()
        {
            if (_stash == null)
            {
                _stash = new Stack<object>();
            }
            _stash.Push(Message);
        }

        public void Respond(object message)
        {
            Sender.Tell(message);
        }

        public PID Spawn(Props<T> props)
        {
            var id = ProcessRegistry.Instance.NextId();
            return SpawnNamed(props, id);
        }

        public PID SpawnPrefix(Props<T> props, string prefix)
        {
            var name = prefix + ProcessRegistry.Instance.NextId();
            return SpawnNamed(props, name);
        }

        public PID SpawnNamed(Props<T> props, string name)
        {
            var pid = props.Spawn($"{Self.Id}/{name}", Self);
            if (_children == null)
            {
                _children = new FastSet<PID>();
            }
            _children.Add(pid);

           return pid;
        }

        public void Watch(PID pid)
        {
            pid.SendSystemMessage(new Watch(Self));
        }

        public void Unwatch(PID pid)
        {
            pid.SendSystemMessage(new Unwatch(Self));
        }

        public void SetReceiveTimeout(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration must be greater than zero");
            }
            
            if (duration == ReceiveTimeout)
            {
                return;
            }
            
            StopReceiveTimeout();
            ReceiveTimeout = duration;
            
            if (_receiveTimeoutTimer == null)
            {
                _receiveTimeoutTimer = new Timer(ReceiveTimeoutCallback, null, ReceiveTimeout, ReceiveTimeout);
            }
            else
            {
                ResetReceiveTimeout();
            }
        }

        public void CancelReceiveTimeout()
        {
            if (_receiveTimeoutTimer == null)
            {
                return;
            }
            StopReceiveTimeout();
            _receiveTimeoutTimer = null;
            ReceiveTimeout = TimeSpan.Zero;
        }

        public Task ReceiveAsync(IMessageEnvelope<T> message)
        {
            return ProcessMessageAsync(message);
        }

        public void Tell<TMessage>(PID target, TMessage message)
        {
            var messageEnvelope = new MessageEnvelope<TMessage>(message, null, null);
            SendUserMessage(target, messageEnvelope);
        }

        public void Request<TRequest>(PID target, TRequest message)
        {
            var messageEnvelope = new MessageEnvelope<TRequest>(message, Self, null);
            SendUserMessage(target, messageEnvelope);
        }

        public Task<TResponse> RequestAsync<TRequest, TResponse>(PID target, TRequest message, TimeSpan timeout)
            => RequestAsync(target, message, new FutureProcess<TResponse>(timeout));

        public Task<TResponse> RequestAsync<TRequest, TResponse>(PID target, TRequest message, CancellationToken cancellationToken)
            => RequestAsync(target, message, new FutureProcess<TResponse>(cancellationToken));

        public Task<TResponse> RequestAsync<TRequest, TResponse>(PID target, TRequest message)
            => RequestAsync(target, message, new FutureProcess<TResponse>());

        public void ReenterAfter<T2>(Task<T2> target, Func<Task<T2>, Task> action)
        {
            var msg = _messageEnvelope;
            var cont = new Continuation(() => action(target), msg);

            target.ContinueWith(t => { Self.SendSystemMessage(cont); });
        }

        public void EscalateFailure(Exception reason, PID who)
        {
            if (_restartStatistics == null)
            {
                _restartStatistics = new RestartStatistics(0, null);
            }
            var failure = new Failure(who, reason, _restartStatistics);
            if (Parent == null)
            {
                HandleRootFailure(failure);
            }
            else
            {
                Self.SendSystemMessage(SuspendMailbox.Instance);
                Parent.SendSystemMessage(failure);
            }
        }

        public void RestartChildren(Exception reason, params PID[] pids)
        {
            foreach (var pid in pids)
            {
                pid.SendSystemMessage(new Restart(reason));
            }
        }

        public void StopChildren(params PID[] pids)
        {
            foreach (var pid in pids)
            {
                pid.SendSystemMessage(Stop.Instance);
            }
        }

        public void ResumeChildren(params PID[] pids)
        {
            foreach (var pid in pids)
            {
                pid.SendSystemMessage(ResumeMailbox.Instance);
            }
        }

        public Task InvokeSystemMessageAsync(object msg)
        {
            try
            {
                switch (msg)
                {
                    //case Started s:
                    //    return InvokeUserMessageAsync(s);
                    case Stop _:
                        return HandleStopAsync();
                    case Terminated t:
                        return HandleTerminatedAsync(t);
                    case Watch w:
                        HandleWatch(w);
                        return Task.FromResult(0);
                    case Unwatch uw:
                        HandleUnwatch(uw);
                        return Task.FromResult(0);
                    case Failure f:
                        HandleFailure(f);
                        return Task.FromResult(0);
                    case Restart _:
                        return HandleRestartAsync();
                    case SuspendMailbox _:
                        return Task.FromResult(0);
                    case ResumeMailbox _:
                        return Task.FromResult(0);
                    //case Continuation cont:
                    //    _message = cont.Message;
                    //    return cont.Action();
                    default:
                        Logger.LogWarning("Unknown system message {0}", msg);
                        return Task.FromResult(0);
                }
            }
            catch (Exception x)
            {
                Logger.LogError("Error handling SystemMessage {0}", x);
                throw;
            }
        }

        public Task InvokeUserMessageAsync(IMessageEnvelope<T> msg)
        {
            var influenceTimeout = true;
            if (ReceiveTimeout > TimeSpan.Zero)
            {
                var notInfluenceTimeout = msg is INotInfluenceReceiveTimeout;
                influenceTimeout = !notInfluenceTimeout;
                if (influenceTimeout)
                {
                    StopReceiveTimeout();
                }
            }

            var res = ProcessMessageAsync(msg);

            if (ReceiveTimeout != TimeSpan.Zero && influenceTimeout)
            {
                //special handle non completed tasks that need to reset ReceiveTimout
                if (!res.IsCompleted)
                {
                    return res.ContinueWith(_ => ResetReceiveTimeout());
                }

                ResetReceiveTimeout();
            }
            return res;
        }

        public void EscalateFailure(Exception reason, object message)
        {
            EscalateFailure(reason, Self);
        }

        internal static Task DefaultReceive(IContext<T> context)
        {
            var c = (LocalContext<T>) context;
            if (c.Message is PoisonPill)
            {
                c.Self.Stop();
                return Proto.Actor.Done;
            }
            return c.Actor.ReceiveAsync(context);
        }

        internal static Task DefaultSender<TMessage>(ISenderContext<TMessage> context, PID target, IMessageEnvelope<TMessage> envelope)
        {
            ((IProcess<IMessageEnvelope<TMessage>>)target.Ref).SendUserMessage(target, envelope);
            return Task.FromResult(0);
        }

        private Task ProcessMessageAsync(IMessageEnvelope<T> msg)
        {
            _messageEnvelope = msg;
            return _receiveMiddleware != null ? _receiveMiddleware(this) : DefaultReceive(this);
        }

        private Task<TResponse> RequestAsync<TRequest, TResponse>(PID target, TRequest message, FutureProcess<TResponse> future)
        {
            var messageEnvelope = new MessageEnvelope<TRequest>(message, future.Pid, null);
            SendUserMessage(target, messageEnvelope);
            return future.Task;
        }

        private void SendUserMessage<TMessage>(PID target, MessageEnvelope<TMessage> message)
        {
            if (_senderMiddleware != null)
            {
                //_senderMiddleware(this, target, message);
            }
            else
            {
                //Default path
                target.Tell(message);
            }
        }

        private void IncarnateActor()
        {
            _state = ContextState.Alive;
            Actor = _producer();
        }

        private async Task HandleRestartAsync()
        {
            _state = ContextState.Restarting;
            //await InvokeUserMessageAsync(Restarting.Instance);
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.Stop();
                }
            }
            await TryRestartOrTerminateAsync();
        }

        private void HandleUnwatch(Unwatch uw)
        {
            _watchers?.Remove(uw.Watcher);
        }

        private void HandleWatch(Watch w)
        {
            if (_state == ContextState.Stopping)
            {
                w.Watcher.SendSystemMessage(new Terminated
                {
                    Who = Self
                });
            }
            else
            {
                if (_watchers == null)
                {
                    _watchers = new FastSet<PID>();
                }
                _watchers.Add(w.Watcher);
            }
        }

        private void HandleFailure(Failure msg)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (Actor is ISupervisorStrategy supervisor)
            {
                supervisor.HandleFailure(this, msg.Who, msg.RestartStatistics, msg.Reason);
                return;
            }
            _supervisorStrategy.HandleFailure(this, msg.Who, msg.RestartStatistics, msg.Reason);
        }

        private async Task HandleTerminatedAsync(Terminated msg)
        {
            _children?.Remove(msg.Who);
            //await InvokeUserMessageAsync(msg);
            await TryRestartOrTerminateAsync();
        }

        private void HandleRootFailure(Failure failure)
        {
            Supervision.DefaultStrategy.HandleFailure(this, failure.Who, failure.RestartStatistics, failure.Reason);
        }

        private async Task HandleStopAsync()
        {
            _state = ContextState.Stopping;
            //this is intentional
            //await InvokeUserMessageAsync(Stopping.Instance);
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.Stop();
                }
            }
            await TryRestartOrTerminateAsync();
        }

        private async Task TryRestartOrTerminateAsync()
        {
            CancelReceiveTimeout();

            if (_children?.Count > 0)
            {
                return;
            }
            
            switch (_state)
            {
                case ContextState.Restarting:
                    await RestartAsync();
                    return;
                case ContextState.Stopping:
                    await StopAsync();
                    break;
            }
        }

        private async Task StopAsync()
        {
            ProcessRegistry.Instance.Remove(Self);
            //This is intentional
            //await InvokeUserMessageAsync(Stopped.Instance);

            DisposeActorIfDisposable();

            //Notify watchers
            if (_watchers != null)
            {
                var terminated = new Terminated
                {
                    Who = Self
                };
                foreach (var watcher in _watchers)
                {
                    watcher.SendSystemMessage(terminated);
                }
            }
            if (Parent != null)
            {
                var terminated = new Terminated
                {
                    Who = Self
                };
                Parent.SendSystemMessage(terminated);
            }
        }

        private async Task RestartAsync()
        {
            DisposeActorIfDisposable();
            IncarnateActor();
            Self.SendSystemMessage(ResumeMailbox.Instance);

            //await InvokeUserMessageAsync(Started.Instance);
            if (_stash != null)
            {
                while (_stash.Any())
                {
                    var msg = _stash.Pop();
                    //await InvokeUserMessageAsync(msg);
                }
            }
        }

        private void DisposeActorIfDisposable()
        {
            if (Actor is IDisposable disposableActor)
            {
                disposableActor.Dispose();
            }
        }

        private void ResetReceiveTimeout()
        {
            _receiveTimeoutTimer?.Change(ReceiveTimeout, ReceiveTimeout);
        }

        private void StopReceiveTimeout()
        {
            _receiveTimeoutTimer?.Change(-1, -1);
        }

        private void ReceiveTimeoutCallback(object state)
        {
            Self.Request(Proto.ReceiveTimeout.Instance, null);
        }
    }
}
