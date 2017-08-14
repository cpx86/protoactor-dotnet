// -----------------------------------------------------------------------
//   <copyright file="MessageEnvelope.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------
namespace Proto
{
    public interface IMessageEnvelope<out T>
    {
        PID Sender { get; }
        T Message { get; }
        MessageHeader Header { get; }
        string GetHeader(string key, string @default = null);
        void SetHeader(string key, string value);
    }

    public struct MessageEnvelope<T> : IMessageEnvelope<T>
    {
        public MessageEnvelope(T message)
            :this(message, null, null)
        {
        }

        public MessageEnvelope(T message, PID sender, MessageHeader header)
        {
            Sender = sender; // ?? throw new ArgumentNullException(nameof(sender));
            Message = message; // ?? throw new ArgumentNullException(nameof(message));
            Header = header;
        }

        public PID Sender { get; }
        public T Message { get; }
        public MessageHeader Header { get; private set; }

        public static (object message, PID sender, MessageHeader headers) Unwrap(object message)
        {
            if (message is MessageEnvelope<T> envelope)
            {
                return (envelope.Message, envelope.Sender, envelope.Header);
            }

            return (message, null, null);
        }

        public string GetHeader(string key, string @default = null)
        {
            if (Header == null)
            {
                return @default;
            }
            return Header.TryGetValue(key, out string value) ? value : @default;
        }

        public void SetHeader(string key, string value)
        {
            if (Header == null)
            {
                Header = new MessageHeader();
            }

            Header[key] = value;
        }
    }
}