// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Asynkron HB">
//      Copyright (C) 2015-2017 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Proto;

class Program
{
    static void Main(string[] args)
    {
        var props = Actor.FromProducer(() => new HelloActor());
        var pid = Actor.Spawn(props);
        pid.Tell(new Hello
        {
            Who = "ProtoActor"
        });

        pid = Actor.Spawn(Actor.FromProducer(() => new AddActor()));
        pid.Tell(13);
        pid.Tell(42);
        pid.Tell(1337);
        Console.ReadLine();
    }

    internal class Hello
    {
        public string Who;
    }

    internal class HelloActor : IActor<Hello>
    {
        public Task ReceiveAsync(IContext<Hello> context)
        {
            var msg = context.Message;
            if (msg is Hello r)
            {
                Console.WriteLine($"Hello {r.Who}");
            }
            return Actor.Done;
        }
    }

    internal class AddActor : IActor<int>
    {
        private int _sum;

        public Task ReceiveAsync(IContext<int> context)
        {
            _sum += context.Message;
            Console.WriteLine(_sum);
            return Actor.Done;
        }
    }
}