using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Greet;
using Proto;

namespace AspNetCore.Server
{
    public class Greeter : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case HelloRequest request:
                    context.Respond(new HelloReply {Message = $"Hello {request.Name}"});
                    break;
            }

            return Actor.Done;
        }
    }
}
