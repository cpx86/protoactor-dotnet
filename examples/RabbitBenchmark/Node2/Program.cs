// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Messages;
using Newtonsoft.Json;
using Proto;
using Proto.RabbitMQ;
using Proto.Remote;
using ProtosReflection = Messages.ProtosReflection;

namespace Node2
{
    public class EchoActor : IActor
    {
        private PID _sender;

        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case StartRemote sr:
                    Console.WriteLine("Starting");
                    _sender = sr.Sender;
                    context.Respond(new Start());
                    return Actor.Done;
                case Ping _:
                    _sender.Tell(new Pong());
                    return Actor.Done;
                default:
                    return Actor.Done;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
            //Remote.Start("127.0.0.1", 12000);
            ProtoRabbit.Init(new ProtoRabbitConfiguration
            {
                Exchange = "proto-exchange-2",
                Queue = "proto-queue-2",
                Serializer = o => Serialization.Serialize(o, Serialization.DefaultSerializerId).ToByteArray(),
                Deserializer = (b, typeName) => Serialization.Deserialize(typeName, ByteString.CopyFrom(b), Serialization.DefaultSerializerId)
            });
            Actor.SpawnNamed(Actor.FromProducer(() => new EchoActor()), "remote");
            Console.ReadLine();
        }
    }
}