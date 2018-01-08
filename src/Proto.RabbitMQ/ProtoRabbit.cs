using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace Proto.RabbitMQ
{
    public class ProtoRabbit
    {
        public static void Main(string[] args)
        {
           
        }

        public static void Init(string exchange, string queue, Func<object, byte[]> serializer, Func<byte[], object> deserializer)
        {
            var connectionFactory = new ConnectionFactory() { HostName = "localhost" };
            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange, "direct", true);
            channel.QueueDeclare(queue, true, true, false);
            channel.QueueBind(queue, exchange, "");

            ProcessRegistry.Instance.RegisterHostResolver(pid => new RabbitProcess(pid, channel, serializer));
            ProcessRegistry.Instance.Address = exchange;
            StartConsumer(channel, queue, deserializer);
        }

        private static void StartConsumer(IModel channel, string queue, Func<byte[], object> deserializer)
        {
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (ch, ea) =>
            {
                var msg = deserializer(ea.Body);
                var id = Encoding.UTF8.GetString((byte[]) ea.BasicProperties.Headers["protoactor-process-id"]);
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

        public RabbitProcess(PID pid, IModel channel, Func<object, byte[]> serializer)
        {
            _pid = pid;
            _channel = channel;
            _serializer = serializer;
        }

        protected override void SendUserMessage(PID pid, object message)
        {
            var body = _serializer(message);
            var props = _channel.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>
            {
                ["protoactor-process-id"] = pid.Id
            };
            _channel.BasicPublish(pid.Address, "", props, body);
        }

        protected override void SendSystemMessage(PID pid, object message)
        {
            var body = _serializer(message);
            var props = _channel.CreateBasicProperties();
            props.Headers["protoactor-process-id"] = pid.Id;
            _channel.BasicPublish(pid.Address, "", props, body);
        }
    }
}
