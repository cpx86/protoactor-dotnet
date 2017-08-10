// -----------------------------------------------------------------------
//   <copyright file="Behavior.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proto
{
    public class Behavior<T>
    {
        private readonly Stack<Receive<T>> _behaviors = new Stack<Receive<T>>();

        public Behavior() { }

        public Behavior(Receive<T> receive)
        {
            Become(receive);
        }

        public void Become(Receive<T> receive)
        {
            _behaviors.Clear();
            _behaviors.Push(receive);
        }

        public void BecomeStacked(Receive<T> receive)
        {
            _behaviors.Push(receive);
        }

        public void UnbecomeStacked()
        {
            _behaviors.Pop();
        }

        public Task ReceiveAsync(IContext<T> context)
        {
            var behavior = _behaviors.Peek();
            return behavior(context);
        }
    }
}