﻿/*
 * Copyright 2020 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Buffers;
using System.Linq;
using Grpc.Core;

namespace GrpcDotNetNamedPipes.Internal
{
    internal class ByteArrayDeserializationContext : DeserializationContext
    {
        private readonly byte[] _payload;

        public ByteArrayDeserializationContext(byte[] payload)
        {
            _payload = payload;
        }

        public override int PayloadLength => _payload.Length;

        public override byte[] PayloadAsNewBuffer() => _payload.ToArray();

        public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() => new ReadOnlySequence<byte>(_payload);
    }
}
