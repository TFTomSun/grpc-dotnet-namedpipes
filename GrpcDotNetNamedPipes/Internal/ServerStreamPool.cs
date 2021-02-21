/*
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

using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace GrpcDotNetNamedPipes.Internal
{
    internal class ServerStreamPool : IDisposable
    {
        public Func<INamedPipeServerStream> Factory { get; }
        private const int PoolSize = 4;
        private const int FallbackMin = 100;
        private const int FallbackMax = 10_000;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Func<INamedPipeServerStream, Task> _handleConnection;
        private bool _started;

        public ServerStreamPool(Func<INamedPipeServerStream> factory,
            Func<INamedPipeServerStream, Task> handleConnection)
        {
            Factory = factory;
            _handleConnection = handleConnection;
        }



        private INamedPipeServerStream CreatePipeServer()
        {

            return Factory();
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            for (int i = 0; i < PoolSize; i++)
            {
                StartListenThread();
            }

            _started = true;
        }

        private void StartListenThread()
        {
            var thread = new Thread(ConnectionLoop);
            thread.Start();
        }

        private void ConnectionLoop()
        {
            int fallback = FallbackMin;
            while (true)
            {
                try
                {
                    ListenForConnection();
                    fallback = FallbackMin;
                }
                catch (Exception)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }
                    // TODO: Log
                    Thread.Sleep(fallback);
                    fallback = Math.Min(fallback * 2, FallbackMax);
                }
            }
        }

        private void ListenForConnection()
        {
            var pipeServer = CreatePipeServer();
            pipeServer.WaitForConnectionAsync(_cts.Token).Wait();
            Task.Run(() =>
            {
                try
                {
                    _handleConnection(pipeServer).Wait();
                    pipeServer.Disconnect();
                }
                catch (Exception)
                {
                    // TODO: Log
                }
                finally
                {
                    pipeServer.Dispose();
                }
            });
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}