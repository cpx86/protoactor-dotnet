﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proto.Mailbox;
using Proto.TestFixtures;
using Xunit;

namespace Proto.Tests
{
    public class SupervisionTests
    {
        class ParentActor : IActor
        {
            private readonly Props _childProps;

            public ParentActor(Props childProps)
            {
                _childProps = childProps;
            }

            public PID Child { get; set; }

            public Task ReceiveAsync(IContext context)
            {
                if (context.Message is Started)
                    Child = context.Spawn(_childProps);
                if (context.Message is string)
                    Child.Tell(context.Message);
                return Actor.Done;
            }
        }

        class ChildActor : IActor
        {

            public Task ReceiveAsync(IContext context)
            {
                switch (context.Message)
                {
                    case string _:
                        throw new Exception();
                }
                return Actor.Done;
            }
        }

        [Fact]
        public void OneForOneStrategy_Should_ResumeChildOnFailure()
        {
            var childMailboxStats = new TestMailboxStatistics(msg => msg is ResumeMailbox);
            var strategy = new OneForOneStrategy((pid, reason) => SupervisorDirective.Resume, 1, TimeSpan.MaxValue);
            var childProps = Actor.FromProducer(() => new ChildActor())
                .WithMailbox(() => UnboundedMailbox.Create(childMailboxStats));
            var parentProps = Actor.FromProducer(() => new ParentActor(childProps))
                .WithSupervisor(strategy);
            var parent = Actor.Spawn(parentProps);

            parent.Tell("hello");

            childMailboxStats.Reset.Wait(1000);
            Assert.Contains(ResumeMailbox.Instance, childMailboxStats.Received);
        }

        [Fact]
        public void OneForOneStrategy_Should_StopChildOnFailure()
        {
            var childMailboxStats = new TestMailboxStatistics(msg => msg is Stopped);
            var strategy = new OneForOneStrategy((pid, reason) => SupervisorDirective.Stop, 1, TimeSpan.MaxValue);
            var childProps = Actor.FromProducer(() => new ChildActor())
                .WithMailbox(() => UnboundedMailbox.Create(childMailboxStats));
            var parentProps = Actor.FromProducer(() => new ParentActor(childProps))
                .WithSupervisor(strategy);
            var parent = Actor.Spawn(parentProps);

            parent.Tell("hello");

            childMailboxStats.Reset.Wait(1000);
            Assert.Contains(Stopped.Instance, childMailboxStats.Received);
        }
    }
}