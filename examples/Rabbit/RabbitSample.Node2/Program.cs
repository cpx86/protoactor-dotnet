using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Proto;
using Proto.RabbitMQ;

namespace RabbitSample.Node2
{
    class Program
    {
        static void Main(string[] args)
        {
            ProtoRabbit.Init("proto-exchange-2", "proto-queue-2",
                o => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o, new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All})),
                b => JsonConvert.DeserializeObject(Encoding.UTF8.GetString(b), new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All})
            );
            var pid = Actor.SpawnNamed(Actor.FromFunc(c =>
            {
                Console.WriteLine(c.Message);
                return Actor.Done;
            }), "actor1");
            Console.ReadLine();
        }
    }
}
