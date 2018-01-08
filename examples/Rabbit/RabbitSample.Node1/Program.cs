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
            ProtoRabbit.Init("proto-exchange-1", "proto-queue-1",
                o => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })),
                b => JsonConvert.DeserializeObject(Encoding.UTF8.GetString(b), new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All})
            );
            var pid = new PID("proto-exchange-2", "actor1");
            tell:
            pid.Tell($"hello {DateTime.Now.Ticks}");
            Console.ReadLine();
            goto tell;
        }
    }
}
