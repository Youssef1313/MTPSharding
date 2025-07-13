using System;
using System.IO;

namespace YTest.MTP.PipeProtocol;

internal sealed class PipeNameDescription(string name, bool isDirectory) : IDisposable
{
    private readonly bool _isDirectory = isDirectory;
    private bool _disposed;

    public string Name { get; } = name;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_isDirectory)
        {
            try
            {
                Directory.Delete(Path.GetDirectoryName(Name)!, true);
            }
            catch (IOException)
            {
                // This folder is created inside the temp directory and will be cleaned up eventually by the OS
            }
        }

        _disposed = true;
    }
}
