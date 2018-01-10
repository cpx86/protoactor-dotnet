using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Proto.Remote;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace Proto.RabbitMQ
{
    public class ProtoRabbitConfiguration
    {
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public Func<object, byte[]> Serializer { get; set; }
        public Func<byte[], string, object> Deserializer { get; set; }
        public Func<RemoteDeliver, string> RoutingKeyFactory { get; set; }
    }

    public class ProtoRabbit
    {
        public static void Main(string[] args)
        {
           
        }

        public static void Init(ProtoRabbitConfiguration protoRabbitConfiguration)
        {
            var channel = CreateChannel(protoRabbitConfiguration.Exchange, protoRabbitConfiguration.Queue);

            ProcessRegistry.Instance.RegisterHostResolver(pid => new RabbitProcess(pid, channel, protoRabbitConfiguration.Serializer, protoRabbitConfiguration.RoutingKeyFactory));
            ProcessRegistry.Instance.Address = protoRabbitConfiguration.Exchange;
            StartConsumer(channel, protoRabbitConfiguration.Queue, protoRabbitConfiguration.Deserializer);
        }

        private static IModel CreateChannel(string exchange, string queue)
        {
            var connectionFactory = new ConnectionFactory() {HostName = "localhost"};
            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange, "direct", true);
            channel.QueueDeclare(queue, true, false, false);
            channel.QueueBind(queue, exchange, "");
            channel.ConfirmSelect();
            return channel;
        }

        private static void StartConsumer(IModel channel, string queue, Func<byte[], string, object> deserializer)
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (ch, ea) =>
            {
                var id = Encoding.UTF8.GetString((byte[]) ea.BasicProperties.Headers["protoactor-process-id"]);
                var typeName = Encoding.UTF8.GetString((byte[]) ea.BasicProperties.Headers["protoactor-message-typename"]);
                var msg = deserializer(ea.Body, typeName);

                if (ea.BasicProperties.Headers.ContainsKey("protoactor-sender-address"))
                {
                    var senderAddress = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["protoactor-sender-address"]);
                    var senderId = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["protoactor-sender-id"]);
                    msg = new MessageEnvelope(msg, new PID(senderAddress, senderId), MessageHeader.EmptyHeader);
                }

                var (pid, ok) = ProcessRegistry.Instance.TryGet(id);
                if(!ok)
                    Console.WriteLine($"Unknown process with ID {id}");
                pid.Tell(msg);
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue, false, consumer);
        }
    }

    public class RabbitProcess : Process
    {
        private readonly PID _pid;
        private readonly IModel _channel;
        private readonly Func<object, byte[]> _serializer;
        private static readonly ConcurrentDictionary<string, PID> Connections = new ConcurrentDictionary<string, PID>();
        private Func<RemoteDeliver, string> _routingKeyFactory;

        public RabbitProcess(PID pid, IModel channel, Func<object, byte[]> serializer, Func<RemoteDeliver, string> routingKeyFactory)
        {
            _pid = pid;
            _channel = channel;
            _serializer = serializer;
            _routingKeyFactory = routingKeyFactory;
        }

        protected override void SendUserMessage(PID pid, object message) => Send(_pid, message);

        protected override void SendSystemMessage(PID pid, object message) => Send(_pid, message);

        private void Send(PID pid, object message)
        {
            PID PublisherFactory(string s) => Actor.Spawn(Actor.FromProducer(() => new RabbitPublisher(_channel, _serializer, _routingKeyFactory)));
            var publisher = Connections.GetOrAdd(pid.Address, PublisherFactory);
            var (msg, sender, header) = Proto.MessageEnvelope.Unwrap(message);
            publisher.Tell(new RemoteDeliver(MessageHeader.EmptyHeader, msg, pid, sender, Serialization.DefaultSerializerId));
        }
    }

    public class RabbitPublisher : IActor
    {
        private readonly IModel _channel;
        private readonly Func<object, byte[]> _serializer;
        private Func<RemoteDeliver, string> _routingKeyFactory;

        public RabbitPublisher(IModel channel, Func<object, byte[]> serializer, Func<RemoteDeliver, string> routingKeyFactory)
        {
            _channel = channel;
            _serializer = serializer;
            _routingKeyFactory = routingKeyFactory;
        }

        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case RemoteDeliver rd:
                    var props = _channel.CreateBasicProperties();
                    props.Headers = new Dictionary<string, object>();
                    if (rd.Sender != null)
                    {
                        props.Headers["protoactor-sender-address"] = rd.Sender.Address;
                        props.Headers["protoactor-sender-id"] = rd.Sender.Id;
                    }
                    props.Headers["protoactor-process-id"] = rd.Target.Id;
                    props.Headers["protoactor-message-typename"] = Serialization.GetTypeName(rd.Message, Serialization.DefaultSerializerId);
                    var body = _serializer(rd.Message);
                    var routingKey = _routingKeyFactory?.Invoke(rd) ?? string.Empty;
                    _channel.BasicPublish(rd.Target.Address, routingKey, props, body);
                    break;
            }
            return Actor.Done;
        }
    }
}
