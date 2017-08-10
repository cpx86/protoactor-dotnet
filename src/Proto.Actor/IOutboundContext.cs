// -----------------------------------------------------------------------
//   <copyright file="IOutboundContext.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------
namespace Proto
{
    public interface ISenderContext<T>
    {
        T Message { get; }

        MessageHeader Headers { get; }
    }
}