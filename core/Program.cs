using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace core
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_VM_READ = 0x0010;

        static void Main(string[] args)
        {
            const string processName = "ac_client";
            const int healthOffset = 0xEC;
            const int playerOffset = 0x0017E0A8;

            Process process = Process.GetProcessesByName(processName).FirstOrDefault();
            if (process == null)
            {
                Console.WriteLine($"Could not find process: {processName}");
                return;
            }
            IntPtr hProcess = OpenProcess(PROCESS_VM_READ, false, process.Id);
            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("Failed to open process.");
                return;
            }

            try
            {
                IntPtr moduleBaseAddress = process.MainModule.BaseAddress;
                Console.WriteLine($"Main Module Base Address: 0x{moduleBaseAddress.ToInt64():X}");

                IntPtr baseAddress = IntPtr.Add(moduleBaseAddress, playerOffset);
                int playerBase = ReadInt(hProcess, baseAddress);
                IntPtr healthAddress = IntPtr.Add((IntPtr)playerBase, healthOffset);

                Console.WriteLine($"Health Address: 0x{healthAddress.ToInt64():X}");

                int healthValue = ReadInt(hProcess, healthAddress);
                Console.WriteLine($"Player Health: {healthValue}");
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        private static int ReadInt(IntPtr hProcess, IntPtr address)
        {
            byte[] buffer = new byte[4];
            int bytesRead;
            if (ReadProcessMemory(hProcess, address, buffer, buffer.Length, out bytesRead) && bytesRead == 4)
            {
                return BitConverter.ToInt32(buffer, 0);
            }
            else
            {
                throw new Exception("Failed to read memory");
            }
        }
    }
}
