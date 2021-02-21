using System.Threading;
using System.Threading.Tasks;
using GrpcDotNetNamedPipes;

namespace System.IO.Pipes
{
    public interface IPipeStream: IDisposable
    {
        Task<int> ReadAsync(byte[] messageBuffer, int i, int messageBufferSize);
        bool IsConnected { get; }
        bool IsMessageComplete { get; }
        void Read(byte[] payload, int i, int payloadLength);
        void Write(byte[] payload, int i, int payloadLength);

        Stream AsWritableStream();
    }

    public interface INamedPipeClientStream : IPipeStream
    {
        void Connect(int connectionTimeout);
        PipeTransmissionMode ReadMode { get; set; }
        void Close();
    }
    public interface INamedPipeServerStream: IPipeStream
    {
        void RunAsClient(PipeStreamImpersonationWorker impersonationWorker);
        Task WaitForConnectionAsync(CancellationToken ctsToken);
        void Disconnect();

     
    }

    public class NamedPipeStreamFactory
    {
        public readonly static int MaxAllowedServerInstances = int.MaxValue;



        public static INamedPipeClientStream CreateClient(string serverName, string pipeName,
            NamedPipeChannelOptions options)
        {
            var pipeOptions = PipeOptions.Asynchronous;
#if NETCOREAPP || NETSTANDARD2_1
            if (options.CurrentUserOnly)
            {
                pipeOptions |= PipeOptions.CurrentUserOnly;
            }
#endif

            var stream = new NamedPipeClientStreamFacade( new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut,
                pipeOptions, options.ImpersonationLevel, HandleInheritability.None));
            return stream;
        }



        public static INamedPipeServerStream Create(string pipeName, NamedPipeServerOptions options)
        {
            var pipeOptions = PipeOptions.Asynchronous;
#if NETCOREAPP || NETSTANDARD
#if !NETSTANDARD2_0
                        if (options.CurrentUserOnly)
                        {
                            pipeOptions |= PipeOptions.CurrentUserOnly;
                        }
#endif

#if NET5_0
                        return  new NamedPipeServerStreamFacade(NamedPipeServerStreamAcl.Create(pipeName,
                            PipeDirection.InOut,
                            NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Message,
                            pipeOptions,
                            0,
                            0,
                            options.PipeSecurity));
#else
                        return  new NamedPipeServerStreamFacade(new NamedPipeServerStream(pipeName,
                            PipeDirection.InOut,
                            NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Message,
                            pipeOptions));
#endif
#endif
#if NETFRAMEWORK
            return new NamedPipeServerStreamFacade(new NamedPipeServerStream(pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                pipeOptions,
                0,
                0,
                options.PipeSecurity));
#endif

        }
    }

    public class PipeStreamFacade : IPipeStream
    {
        public PipeStream PipeStream { get; }

        public PipeStreamFacade(PipeStream pipeStream)
        {
            PipeStream = pipeStream;
        }


        public void Dispose() => PipeStream.Dispose();
        public Task<int> ReadAsync(byte[] messageBuffer, int i, int messageBufferSize)
        {
            return PipeStream.ReadAsync(messageBuffer, i, messageBufferSize);
        }

        public bool IsConnected => PipeStream.IsConnected;

        public bool IsMessageComplete
        {
            get => PipeStream.IsMessageComplete;
        }
        public void Read(byte[] payload, int i, int payloadLength)
        {
            PipeStream.Read(payload, i, payloadLength);
        }

        public void Write(byte[] payload, int i, int payloadLength)
        {
            PipeStream.Write(payload, i, payloadLength);
        }

        public Stream AsWritableStream()
        {
            return PipeStream;
        }
    }

    public class NamedPipeServerStreamFacade : PipeStreamFacade, INamedPipeServerStream
    {
        public NamedPipeServerStream PipeStream { get; }

        public NamedPipeServerStreamFacade(NamedPipeServerStream pipeStream) : base(pipeStream)
        {
            PipeStream = pipeStream;
        }

        public void RunAsClient(PipeStreamImpersonationWorker impersonationWorker)
        {
            PipeStream.RunAsClient(impersonationWorker);
        }

        public Task WaitForConnectionAsync(CancellationToken ctsToken)
        {

            return PipeStream.WaitForConnectionAsync(ctsToken);
        }

        public void Disconnect()
        {
            PipeStream.Disconnect();
        }
    }

    public class NamedPipeClientStreamFacade : PipeStreamFacade, INamedPipeClientStream
    {
        private readonly NamedPipeClientStream _clientStream;

        public NamedPipeClientStreamFacade(NamedPipeClientStream clientStream) : base(clientStream)
        {
            _clientStream = clientStream;
        }


        public void Connect(int connectionTimeout)
        {
            _clientStream.Connect(connectionTimeout);
        }

        public PipeTransmissionMode ReadMode
        {
            get => _clientStream.ReadMode;
            set => _clientStream.ReadMode = value;
        }
        public void Close()
        {
            _clientStream.Close();
        }
    }

}
