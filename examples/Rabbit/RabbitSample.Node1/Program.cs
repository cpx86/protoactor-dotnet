using System;
using System.Text;
using Newtonsoft.Json;
using Proto;
using Proto.RabbitMQ;

namespace RabbitSample
{
    class Program
    {
        static void Main(string[] args)
        {
            ProtoRabbit.Init(new ProtoRabbitConfiguration
            {
                Exchange = "proto-exchange-1",
                Queue = "proto-queue-1",
                Serializer = o => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })),
                Deserializer = (b, typeName) => JsonConvert.DeserializeObject(Encoding.UTF8.GetString(b), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })
            });
            var pid = new PID("proto-exchange-2", "actor1");
            tell:
            pid.Tell($"hello {DateTime.Now.Ticks}");
            Console.ReadLine();
            goto tell;
        }
    }
}
