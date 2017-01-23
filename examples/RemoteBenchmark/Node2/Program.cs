// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Asynkron HB">
//      Copyright (C) 2015-2016 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using Proto;
using Proto.Remote;
using ProtosReflection = Messages.ProtosReflection;

namespace Node2
{
    public class EchoActor : IActor
    {
        private int _count;
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
                    _count++;
                    if (_count % 50000 == 0)
                    {
                        Console.WriteLine(_count);
                    }
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
            var remoteStats = new MailboxStats();
            RemotingSystem.Start("127.0.0.1", 12001, remoteStats);
            var pongActorStats = new MailboxStats();
            var props = Actor.FromProducer(() => new EchoActor())
                .WithMailbox(() => new DefaultMailbox(new BoundedMailboxQueue(32), new BoundedMailboxQueue(1024*1024), pongActorStats));
            Actor.SpawnNamed(props, "remote");
            Console.ReadLine();
            Console.WriteLine($"Pong actor Received:{pongActorStats.Received} Posted:{pongActorStats.Posted}");
            Console.WriteLine($"Remote system Received:{remoteStats.Received} Posted:{remoteStats.Posted}");
            Console.ReadLine();
        }
    }
}

internal class MailboxStats : IMailboxStatistics
{
    private int _posted;
    private int _received;

    public int Posted => _posted;
    public int Received => _received;

    public void MailboxStarted()
    {
    }

    public void MessagePosted(object message)
    {
        Interlocked.Increment(ref _posted);
    }

    public void MessageReceived(object message)
    {
        Interlocked.Increment(ref _received);
    }

    public void MailboxEmpty()
    {
    }
}