using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.NetworkInformation;

namespace OpenHabitTracker.Blazor.Chromely;

public static class ServerAppUtil
{
    private const int _defaultPort = 5001;
    private const int _startScan = 5050;
    private const int _endScan = 6000;
    private const string _argumentType = "--type";

    public static Task? BlazorTask { get; set; }
    public static CancellationTokenSource? BlazorTaskTokenSource { get; set; }

    public static int AvailablePort
    {
        get
        {
            for (int i = _startScan; i < _endScan; i++)
            {
                if (IsPortAvailable(i))
                {
                    return i;
                }
            }

            return _defaultPort;
        }
    }

    public static bool IsMainProcess(IEnumerable<string> args)
    {
        if (args == null || !args.Any())
        {
            return true;
        }

        if (!HasArgument(args, _argumentType))
        {
            return true;
        }

        return false;
    }

    public static bool IsPortAvailable(int port)
    {
        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

        foreach (IPEndPoint endpoint in tcpConnInfoArray)
        {
            if (endpoint.Port == port)
            {
                return false;
            }
        }

        return true;
    }

    public static void SavePort(string appName, int port)
    {
        // used to pass the port number to chromely child processes
        MemoryMappedFile mmf = MemoryMappedFile.CreateNew(appName, 4);
        MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();
        accessor.Write(0, port);
    }

    public static int GetSavedPort(string appName)
    {
        MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(appName, 4);
        MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();
        return accessor.ReadInt32(0);
    }

    public static void ProcessExit(object? sender, EventArgs e)
    {
        // Clean up kestrel process if not taken down by OS. This can
        // occur when the app is closed from WindowController (frameless).
        Task.Run(() =>
        {
            if (BlazorTaskTokenSource != null)
            {
                WaitHandle.WaitAny([BlazorTaskTokenSource.Token.WaitHandle]);
            }

            BlazorTask?.Dispose();
        });
        BlazorTaskTokenSource?.Cancel();
    }

    private static bool HasArgument(IEnumerable<string> args, string arg)
    {
        return args.Any(a => a.StartsWith(arg));
    }
}