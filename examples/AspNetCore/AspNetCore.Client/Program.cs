using System;
using System.Threading.Tasks;
using Greet;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Remote;

namespace AspNetCore.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Serialization.RegisterFileDescriptor(GreetReflection.Descriptor);
            Remote.Start("127.0.0.1", 0);
            var context = RootContext.Empty;
            Log.SetLoggerFactory(new LoggerFactory().AddConsole(LogLevel.Debug));
            var server = new PID("127.0.0.1:54388", "greeter");
            EventStream.Instance.Subscribe<DeadLetterEvent>(deadLetter =>
                Console.WriteLine($"Dead-letter: {deadLetter.Pid} -> {deadLetter.Sender}: {deadLetter.Message}"));

            send:
            Console.WriteLine("Send?");
            Console.ReadLine();

            var reply = await context.RequestAsync<HelloReply>(server, new HelloRequest {Name = "Client"});
            Console.WriteLine(reply.Message);
            goto send;
        }
    }
}
