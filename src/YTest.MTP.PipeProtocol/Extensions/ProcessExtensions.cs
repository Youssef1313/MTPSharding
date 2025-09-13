#if !NET
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
#endif

namespace YTest.MTP.PipeProtocol;

internal static class ProcessExtensions
{
#if !NET
    public static async Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
    {
        if (!process.HasExited)
        {
            // Early out for cancellation before doing more expensive work
            cancellationToken.ThrowIfCancellationRequested();
        }

        try
        {
            // CASE 1: We enable events
            // CASE 2: Process exits before enabling events (and throws an exception)
            // CASE 3: User already enabled events (no-op)
            process.EnableRaisingEvents = true;
        }
        catch (InvalidOperationException)
        {
            // CASE 2: If the process has exited, our work is done, otherwise bubble the
            // exception up to the user
            if (process.HasExited)
            {
                // await WaitUntilOutputEOF(cancellationToken).ConfigureAwait(false);
                return;
            }

            throw;
        }

        //async Task WaitUntilOutputEOF(CancellationToken cancellationToken)
        //{
        //    if (_output is not null)
        //    {
        //        await _output.EOF.WaitAsync(cancellationToken).ConfigureAwait(false);
        //    }

        //    if (_error is not null)
        //    {
        //        await _error.EOF.WaitAsync(cancellationToken).ConfigureAwait(false);
        //    }
        //}

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler handler = (_, _) => tcs.TrySetResult(null);
        process.Exited += handler;

        try
        {
            if (process.HasExited)
            {
                // CASE 1.2 & CASE 3.2: Handle race where the process exits before registering the handler
            }
            else
            {
                // CASE 1.1 & CASE 3.1: Process exits or is canceled here
                // NOTE: dotnet/runtime calls UnsafeRegister here instead!
                using (cancellationToken.Register(static state =>
                {
                    var (tcs, cancellationToken) = ((TaskCompletionSource<object?>, CancellationToken))state!;
                    tcs.TrySetCanceled(cancellationToken);
                }, (tcs, cancellationToken)))
                {
                    await tcs.Task.ConfigureAwait(false);
                }
            }

            // Wait until output streams have been drained
            // await WaitUntilOutputEOF(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            process.Exited -= handler;
        }
    }
#endif
}
