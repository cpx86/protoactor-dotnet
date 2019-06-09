using System;
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
                grpc.
            });
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
            
            app.UseEndpoints(endpoints =>
            {
                var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
                //loggerFactory.AddProvider(new ConsoleLoggerProvider(console => { }));
                Log.SetLoggerFactory(loggerFactory);

                endpoints.MapProtoRemote(remote =>
                {
                    remote.RegisterDescriptor(GreetReflection.Descriptor);
                    remote.EndpointWriterBatchSize = 100;
                });
                
                endpoints.MapGrpcService<EndpointReader>();
                Serialization.RegisterFileDescriptor(GreetReflection.Descriptor);
                ProcessRegistry.Instance.RegisterHostResolver(pid => new RemoteProcess(pid));
                EndpointManager.Start();
                ProcessRegistry.Instance.Address = $"localhost:{54388}";

                RootContext.Empty.SpawnNamed(Props.FromProducer(() => new Greeter()), "greeter");
                EventStream.Instance.Subscribe<DeadLetterEvent>(deadLetter =>
                Console.WriteLine($"Dead-letter: {deadLetter.Pid} -> {deadLetter.Sender}: {deadLetter.Message}"));
            });
        }
    }
}