using System.Diagnostics;
using System.Runtime.CompilerServices;
using TinyWatcher.Interop;

namespace TinyWatcher.Recording;

internal sealed class ProcessJob : IDisposable
{
    private nint handle;

    public ProcessJob()
    {
        handle = Kernel32.CreateJobObject(0, 0);
        if (handle == 0)
        {
            return;
        }

        var info = new Kernel32.JobObjectExtendedLimitInformation();
        info.BasicLimitInformation.LimitFlags = Kernel32.JobObjectLimitKillOnJobClose;
        Kernel32.SetInformationJobObject(
            handle,
            Kernel32.JobObjectExtendedLimitInformationClass,
            in info,
            (uint)Unsafe.SizeOf<Kernel32.JobObjectExtendedLimitInformation>());
    }

    public void Assign(Process process)
    {
        if (handle != 0)
        {
            Kernel32.AssignProcessToJobObject(handle, process.Handle);
        }
    }

    public void Dispose()
    {
        if (handle != 0)
        {
            Kernel32.CloseHandle(handle);
            handle = 0;
        }
    }
}
