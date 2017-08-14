// -----------------------------------------------------------------------
//   <copyright file="RootContext.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Proto
{
    public class ActorClient<T> : ISenderContext<T>
    {
        private readonly Sender<T> _senderMiddleware;

        public ActorClient(MessageHeader messageHeader, params Func<Sender<T>, Sender<T>>[] middleware)
        {
            _senderMiddleware = middleware.Reverse()
                    .Aggregate((Sender<T>)DefaultSender, (inner, outer) => outer(inner));
            Headers = messageHeader;
        }

        public T Message => default(T);
        public MessageHeader Headers { get; }

        private Task DefaultSender(ISenderContext<T> context, PID target, IMessageEnvelope<T> message)
        {
            target.Tell(message);
            return Actor.Done;
        }

        public void Tell(PID target, T message)
        {
            if (_senderMiddleware != null)
            {
               
                _senderMiddleware(this, target, new MessageEnvelope<T>(message, null, null));
            }
            else
            {
                //Default path
                target.Tell(new MessageEnvelope<T>(message, null, null));
            }
        }

        public void Request(PID target, T message, PID sender)
        {
            var envelope = new MessageEnvelope<T>(message, sender, null);
            if (_senderMiddleware != null)
            {

                _senderMiddleware(this, target, envelope);
            }
            else
            {
                target.Tell(envelope);
            }
        }

        public Task<T> RequestAsync<T>(PID target, object message, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public Task<T> RequestAsync<T>(PID target, object message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T> RequestAsync<T>(PID target, object message)
        {
            throw new NotImplementedException();
        }
    }
}
