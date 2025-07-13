using System;
using System.Threading;
using System.Threading.Tasks;

namespace YTest.MTP.PipeProtocol;

internal interface IServer : INamedPipeBase,
#if NETCOREAPP
IAsyncDisposable,
#endif
IDisposable
{
    PipeNameDescription PipeName { get; }

    Task WaitConnectionAsync(CancellationToken cancellationToken);
}
