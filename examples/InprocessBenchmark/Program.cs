﻿// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Asynkron HB">
//      Copyright (C) 2015-2016 Asynkron HB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Proto;
using Proto.Mailbox;
using static Proto.Actor;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"Is Server GC {GCSettings.IsServerGC}");
        const int messageCount = 1000000;
        const int batchSize = 100;

        Console.WriteLine("Dispatcher\t\tElapsed\t\tMsg/sec");
        var tps = new[] {300, 400, 500, 600, 700, 800, 900};
        foreach (var t in tps)
        {
            var d = new ThreadPoolDispatcher {Throughput = t};

            var clientCount = Environment.ProcessorCount * 1;
            var clients = new PID[clientCount];
            var echos = new PID[clientCount];
            var completions = new TaskCompletionSource<bool>[clientCount];

            var echoProps = FromProducer(() => new EchoActor())
                .WithDispatcher(d)
                .WithMailbox(() => BoundedMailbox.Create<IMessageEnvelope<object>>(2048));

            for (var i = 0; i < clientCount; i++)
            {
                var tsc = new TaskCompletionSource<bool>();
                completions[i] = tsc;
                var clientProps = FromProducer(() => new PingActor(tsc, messageCount, batchSize))
                    .WithDispatcher(d)
                    .WithMailbox(() => BoundedMailbox.Create<IMessageEnvelope<object>>(2048));

                clients[i] = Spawn(clientProps);
                echos[i] = Spawn(echoProps);
            }
            var tasks = completions.Select(tsc => tsc.Task).ToArray();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < clientCount; i++)
            {
                var client = clients[i];
                var echo = echos[i];

                client.Tell(new Start(echo));
            }
            Task.WaitAll(tasks);

            sw.Stop();
            var totalMessages = messageCount * 2 * clientCount;

            var x = (int) (totalMessages / (double) sw.ElapsedMilliseconds * 1000.0d);
            Console.WriteLine($"{t}\t\t\t{sw.ElapsedMilliseconds}\t\t{x}");
            Thread.Sleep(2000);
        }

        Console.ReadLine();
    }

    public class Msg
    {
        public Msg(PID sender)
        {
            Sender = sender;
        }

        public PID Sender { get; }
    }

    public class Start
    {
        public Start(PID sender)
        {
            Sender = sender;
        }

        public PID Sender { get; }
    }

    public class EchoActor : IActor<object>
    {
        public Task ReceiveAsync(IContext<object> context)
        {
            switch (context.Message)
            {
                case Msg msg:
                    msg.Sender.Tell(msg);
                    break;
            }
            return Done;
        }
    }


    public class PingActor : IActor<object>
    {
        private readonly int _batchSize;
        private readonly TaskCompletionSource<bool> _wgStop;
        private int _batch;
        private int _messageCount;

        public PingActor(TaskCompletionSource<bool> wgStop, int messageCount, int batchSize)
        {
            _wgStop = wgStop;
            _messageCount = messageCount;
            _batchSize = batchSize;
        }

        public Task ReceiveAsync(IContext<object> context)
        {
            switch (context.Message)
            {
                case Start s:
                    SendBatch(context, s.Sender);
                    break;
                case Msg m:
                    _batch--;

                    if (_batch > 0)
                    {
                        break;
                    }

                    if (!SendBatch(context, m.Sender))
                    {
                        _wgStop.SetResult(true);
                    }
                    break;
            }
            return Done;
        }

        private bool SendBatch(IContext<object> context, PID sender)
        {
            if (_messageCount == 0)
            {
                return false;
            }

            var m = new Msg(context.Self);
            
            for (var i = 0; i < _batchSize; i++)
            {
                sender.Tell(m);
            }

            _messageCount -= _batchSize;
            _batch = _batchSize;
            return true;
        }
    }
}