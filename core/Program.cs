using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace core
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // private const uint PROCESS_VM_READ = 0x0010;
        private const uint ALL = 0x001F0FFF;

        static void Main(string[] args)
        {
            const string processName = "ac_client";

            Process process = Process.GetProcessesByName(processName).FirstOrDefault();
            if (process == null)
            {
                Console.WriteLine($"Could not find process: {processName}");
                return;
            }
            IntPtr hProcess = OpenProcess(ALL, false, process.Id);
            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("Failed to open process.");
                return;
            }

            IntPtr moduleBaseAddress = process.MainModule.BaseAddress;
            Console.WriteLine($"Main Module Base Address: 0x{moduleBaseAddress.ToInt64():X}");

            // 서버 정보
            string serverHost = "127.0.0.1";
            int serverPort = 3000;

            // 서버 주소 생성
            IPAddress ipAddress = IPAddress.Parse(serverHost);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

            // 소켓 생성
            Socket listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

            // 서버 시작
            listener.Bind(remoteEP);
            listener.Listen(10);

            Console.WriteLine("Waiting for a connection...");

            while (true)
            {
                try
                {
                    // 연결 요청 대기
                    Socket handler = listener.Accept();

                    Console.WriteLine($"Connected to {((IPEndPoint)handler.RemoteEndPoint).Address}");

                    // 클라이언트로부터 받은 데이터 읽기
                    byte[] data = new byte[64];
                    int bytesRec = handler.Receive(data);
                    string requestData = Encoding.ASCII.GetString(data, 0, bytesRec);
                    Console.WriteLine($"Received data from client: {requestData}");

                    // 클라이언트에 응답 보내기
                    if (requestData == "getHealth")
                    {
                        int playerHealth = getPlayerHealth(hProcess, moduleBaseAddress);
                        string responseData = $"Player Health : {playerHealth}";
                        byte[] msg = Encoding.ASCII.GetBytes(responseData);
                        handler.Send(msg);
                    }
                    else if (requestData.StartsWith("setHealth:"))
                    {
                        string[] parts = requestData.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int newHealth))
                        {
                            setPlayerHealth(hProcess, moduleBaseAddress, newHealth);
                            byte[] msg = Encoding.ASCII.GetBytes("Health updated successfully");
                            handler.Send(msg);
                        }
                        else
                        {
                            byte[] msg = Encoding.ASCII.GetBytes("Invalid request format");
                            handler.Send(msg);
                        }
                    }

                    else if (requestData == "exit")
                    {
                        CloseHandle(hProcess);
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        break;

                    }

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private static int getPlayerHealth(IntPtr hProcess, IntPtr moduleBaseAddress)
        {
            const int healthOffset = 0xEC;
            const int playerOffset = 0x0017E0A8;

            try
            {
                IntPtr baseAddress = IntPtr.Add(moduleBaseAddress, playerOffset);
                int playerBase = ReadInt(hProcess, baseAddress);
                IntPtr healthAddress = IntPtr.Add((IntPtr)playerBase, healthOffset);

                Console.WriteLine($"Health Address: 0x{healthAddress.ToInt64():X}");

                int healthValue = ReadInt(hProcess, healthAddress);

                return healthValue;
            }
            catch
            {
                return -1;
            }
        }

        private static void setPlayerHealth(IntPtr hProcess, IntPtr moduleBaseAddress, int newHealth)
        {
            const int healthOffset = 0xEC;
            const int playerOffset = 0x0017E0A8;

            IntPtr baseAddress = IntPtr.Add(moduleBaseAddress, playerOffset);
            int playerBase = ReadInt(hProcess, baseAddress);
            IntPtr healthAddress = IntPtr.Add((IntPtr)playerBase, healthOffset);

            Console.WriteLine($"Health Address: 0x{healthAddress.ToInt64():X}");

            byte[] healthBytes = BitConverter.GetBytes(newHealth);
            int bytesWritten;
            if (!WriteProcessMemory(hProcess, healthAddress, healthBytes, healthBytes.Length, out bytesWritten) || bytesWritten != healthBytes.Length)
            {
                throw new Exception("Failed to write memory");
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
