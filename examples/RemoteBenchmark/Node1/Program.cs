using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proto;
using Proto.Remote;
using ProtosReflection = Messages.ProtosReflection;

class Program
{
    public class LocalActor : IActor
    {
        private int _count;
        private AutoResetEvent _wg;
        private int _messageCount;

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
                case Messages.Pong _:
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
    static void Main(string[] args)
    {
        Serialization.RegisterFileDescriptor(ProtosReflection.Descriptor);
        var remoteStats = new MailboxStats();
        RemotingSystem.Start("127.0.0.1", 12000, remoteStats);

        var messageCount = 2*1000*1000;
        var wg = new AutoResetEvent(false);
        var pingActorStats = new MailboxStats();
        var props = Actor
            .FromProducer(() => new LocalActor(0, messageCount, wg))
            .WithMailbox(() => new DefaultMailbox(new BoundedMailboxQueue(32), new BoundedMailboxQueue(1024*1024), pingActorStats));

        var pid = Actor.Spawn(props);
        var remote = new PID("127.0.0.1:12001", "remote");
        remote.RequestAsync<Messages.Start>(new Messages.StartRemote() {Sender = pid}).Wait();

        var start = DateTime.Now;
        Console.WriteLine("Starting to send");
        var msg = new Messages.Ping();
        for (int i = 0; i < messageCount; i++)
        {
            remote.Tell(msg);
        }
        wg.WaitOne(30000);
        var elapsed = DateTime.Now - start;
        Console.WriteLine("Elapsed {0}",elapsed);

        var t = ((messageCount * 2.0) / elapsed.TotalMilliseconds) * 1000;
        Console.WriteLine("Throughput {0} msg / sec",t);

        Console.WriteLine($"Ping actor Received:{pingActorStats.Received} Posted:{pingActorStats.Posted}");
        Console.WriteLine($"Remote system Received:{remoteStats.Received} Posted:{remoteStats.Posted}");

        Console.ReadLine();
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