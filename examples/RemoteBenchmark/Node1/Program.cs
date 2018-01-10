﻿// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Messages;
using Newtonsoft.Json;
using Proto;
using Proto.RabbitMQ;
using Proto.Remote;
using ProtosReflection = Messages.ProtosReflection;

class Program
{
    static void Main(string[] args)
    {
        Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
        //Remote.Start("127.0.0.1", 12001);

        ProtoRabbit.Init(//o => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })),
            //b => JsonConvert.DeserializeObject(Encoding.UTF8.GetString(b), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })
            new ProtoRabbitConfiguration(new InClassName(new InClassName(new ProtoRabbitConfiguration("proto-exchange-1", "proto-queue-1", o => Serialization.Serialize(o, Serialization.DefaultSerializerId).ToByteArray(), (b, type) => Serialization.Deserialize(type, ByteString.CopyFrom(b), Serialization.DefaultSerializerId))))));

        var messageCount = 1000*10;
        var wg = new AutoResetEvent(false);
        var props = Actor
            .FromProducer(() => new LocalActor(0, messageCount, wg));

        var pid = Actor.Spawn(props);
        var remote = new PID("proto-exchange-2", "remote");
        remote.RequestAsync<Start>(new StartRemote {Sender = pid}).Wait();

        var start = DateTime.Now;
        Console.WriteLine("Starting to send");
        var msg = new Ping();
        for (var i = 0; i < messageCount; i++)
        {
            remote.Tell(msg);
        }
        wg.WaitOne();
        var elapsed = DateTime.Now - start;
        Console.WriteLine("Elapsed {0}", elapsed);

        var t = messageCount * 2.0 / elapsed.TotalMilliseconds * 1000;
        Console.WriteLine("Throughput {0} msg / sec", t);

        Console.ReadLine();
    }

    public class LocalActor : IActor
    {
        private int _count;
        private readonly int _messageCount;
        private readonly AutoResetEvent _wg;

        public LocalActor(int count, int messageCount, AutoResetEvent wg)
        {
            _count = count;
            _messageCount = messageCount;
            _wg = wg;
        }


        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Pong _:
                    _count++;
                    if (_count % 50000 == 0)
                    {
                        Console.WriteLine(_count);
                    }
                    if (_count == _messageCount)
                    {
                        _wg.Set();
                    }
                    break;
            }
            return Actor.Done;
        }
    }
}