using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using Greet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Proto;
using Proto.Remote;

namespace AspNetCore.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(grpc =>
            {
                
            });
            services.AddProtoActor();
            services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseProtoRemote((remote, serialization) =>
            {
                serialization.RegisterFileDescriptor(GreetReflection.Descriptor);

            });
            
            app.UseEndpoints(endpoints =>
            {
                //var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
                //loggerFactory.AddProvider(new ConsoleLoggerProvider(console => { }));
                //Log.SetLoggerFactory(loggerFactory);

                //endpoints.MapProtoRemote(remote =>
                //{
                //    remote.RegisterDescriptor(GreetReflection.Descriptor);
                //    remote.EndpointWriterBatchSize = 100;
                //});
                //Remote.Configure(new RemoteConfig());
                endpoints.MapGrpcService<EndpointReader>();
                //Serialization.RegisterFileDescriptor(GreetReflection.Descriptor);

                //ProcessRegistry.Instance.RegisterHostResolver(pid => new RemoteProcess(pid));
                //EndpointManager.Start();
                //ProcessRegistry.Instance.Address = $"localhost:{54388}";

                //RootContext.Empty.SpawnNamed(Props.FromProducer(() => new Greeter()), "greeter");
                //EventStream.Instance.Subscribe<DeadLetterEvent>(deadLetter =>
                //Console.WriteLine($"Dead-letter: {deadLetter.Pid} -> {deadLetter.Sender}: {deadLetter.Message}"));
            });
        }
    }

    internal class ProtoRemoteService : IHostedService
    {
        private Remote _remote;

        public ProtoRemoteService(Remote remote)
        {
            _remote = remote;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _remote.
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseProtoRemote(this IApplicationBuilder app, Action<Remote, Serialization> configure)
        {
            var system = app.ApplicationServices.GetRequiredService<ActorSystem>();
            //var system = new ActorSystem();
            var serialization = new Serialization();
            var remote = new Remote(system, serialization);
            configure(remote, serialization);
            ProcessRegistry.Instance.Address = $"localhost:{54388}";

            return app;
        }

        public static void AddProtoActor(this IServiceCollection services)
        {
            ActorSystem system = new ActorSystem();
            services.AddSingleton(system);
            services.AddSingleton(new Remote(system, new Serialization()));
            services.AddHostedService<ProtoRemoteService>();
        }
    }

    internal class RemoteBuilder
    {
        private ActorSystem system;
        private Remote remote;
        private Serialization serialization;

        public RemoteBuilder(ActorSystem system)
        {
            this.system = system;
            this.serialization = new Serialization();
            this.remote = new Remote(system, this.serialization);
        }

        internal void RegisterFileDescriptor(FileDescriptor descriptor)
        {
            serialization.RegisterFileDescriptor(descriptor);
        }
    }
}