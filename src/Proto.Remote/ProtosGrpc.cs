// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Protos.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace Proto.Remote {
  public static partial class Remoting
  {
    static readonly string __ServiceName = "remote.Remoting";

    static readonly grpc::Marshaller<global::Proto.Remote.ConnectRequest> __Marshaller_remote_ConnectRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Proto.Remote.ConnectRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Proto.Remote.ConnectResponse> __Marshaller_remote_ConnectResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Proto.Remote.ConnectResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Proto.Remote.MessageBatch> __Marshaller_remote_MessageBatch = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Proto.Remote.MessageBatch.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Proto.Remote.Unit> __Marshaller_remote_Unit = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Proto.Remote.Unit.Parser.ParseFrom);

    static readonly grpc::Method<global::Proto.Remote.ConnectRequest, global::Proto.Remote.ConnectResponse> __Method_Connect = new grpc::Method<global::Proto.Remote.ConnectRequest, global::Proto.Remote.ConnectResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Connect",
        __Marshaller_remote_ConnectRequest,
        __Marshaller_remote_ConnectResponse);

    static readonly grpc::Method<global::Proto.Remote.MessageBatch, global::Proto.Remote.Unit> __Method_Receive = new grpc::Method<global::Proto.Remote.MessageBatch, global::Proto.Remote.Unit>(
        grpc::MethodType.DuplexStreaming,
        __ServiceName,
        "Receive",
        __Marshaller_remote_MessageBatch,
        __Marshaller_remote_Unit);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Proto.Remote.ProtosReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of Remoting</summary>
    [grpc::BindServiceMethod(typeof(Remoting), "BindService")]
    public abstract partial class RemotingBase
    {
      public virtual global::System.Threading.Tasks.Task<global::Proto.Remote.ConnectResponse> Connect(global::Proto.Remote.ConnectRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task Receive(grpc::IAsyncStreamReader<global::Proto.Remote.MessageBatch> requestStream, grpc::IServerStreamWriter<global::Proto.Remote.Unit> responseStream, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Client for Remoting</summary>
    public partial class RemotingClient : grpc::ClientBase<RemotingClient>
    {
      /// <summary>Creates a new client for Remoting</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public RemotingClient(grpc::Channel channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for Remoting that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public RemotingClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected RemotingClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected RemotingClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual global::Proto.Remote.ConnectResponse Connect(global::Proto.Remote.ConnectRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Connect(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Proto.Remote.ConnectResponse Connect(global::Proto.Remote.ConnectRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Connect, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Proto.Remote.ConnectResponse> ConnectAsync(global::Proto.Remote.ConnectRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ConnectAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Proto.Remote.ConnectResponse> ConnectAsync(global::Proto.Remote.ConnectRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Connect, null, options, request);
      }
      public virtual grpc::AsyncDuplexStreamingCall<global::Proto.Remote.MessageBatch, global::Proto.Remote.Unit> Receive(grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Receive(new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncDuplexStreamingCall<global::Proto.Remote.MessageBatch, global::Proto.Remote.Unit> Receive(grpc::CallOptions options)
      {
        return CallInvoker.AsyncDuplexStreamingCall(__Method_Receive, null, options);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override RemotingClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new RemotingClient(configuration);
      }
    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static grpc::ServerServiceDefinition BindService(RemotingBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_Connect, serviceImpl.Connect)
          .AddMethod(__Method_Receive, serviceImpl.Receive).Build();
    }

    /// <summary>Register service method with a service binder with or without implementation. Useful when customizing the  service binding logic.
    /// Note: this method is part of an experimental API that can change or be removed without any prior notice.</summary>
    /// <param name="serviceBinder">Service methods will be bound by calling <c>AddMethod</c> on this object.</param>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static void BindService(grpc::ServiceBinderBase serviceBinder, RemotingBase serviceImpl)
    {
      serviceBinder.AddMethod(__Method_Connect, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::Proto.Remote.ConnectRequest, global::Proto.Remote.ConnectResponse>(serviceImpl.Connect));
      serviceBinder.AddMethod(__Method_Receive, serviceImpl == null ? null : new grpc::DuplexStreamingServerMethod<global::Proto.Remote.MessageBatch, global::Proto.Remote.Unit>(serviceImpl.Receive));
    }

  }
}
#endregion
