﻿// -----------------------------------------------------------------------
//   <copyright file="Cluster.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proto.Remote;

namespace Proto.Cluster
{
    public static class Cluster
    {
        internal static ILogger Logger { get; } = Log.CreateLogger("Cluster");

        public static void Start(string clusterName, IClusterProvider provider)
        {
            Logger.LogInformation("Starting Proto.Actor cluster");
            var (h, p) = ParseAddress(ProcessRegistry.Instance.Address);
            var kinds = Remote.Remote.GetKnownKinds();
            Partition.SpawnPartitionActors(kinds);
            Partition.SubscribeToEventStream();
            PidCache.Spawn();
            MemberList.Spawn();
            MemberList.SubscribeToEventStream();
            provider.RegisterMemberAsync(clusterName, h, p, kinds).Wait();
            provider.MonitorMemberStatusChanges();
        }

        private static (string host,int port) ParseAddress(string address)
        {
            //TODO: use correct parsing
            var parts = address.Split(':');
            var host = parts[0];
            var port = int.Parse(parts[1]);
            return (host, port);
        }

        public static async Task<PID> GetAsync(string name, string kind)
        {
            var req = new ActorPidRequest()
                      {
                          Name = name,
                          Kind = kind,
                      };
            var res = await PidCache.PID.RequestAsync<ActorPidResponse>(req);
            return res.Pid;
        }
    }
}