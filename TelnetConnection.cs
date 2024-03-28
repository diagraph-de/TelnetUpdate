using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TelnetUpdate
{
    internal class TelnetConnection
    {
        private readonly TcpClient tcpSocket;

        private int TimeOutMs = 1000;

        public TelnetConnection(string Hostname, int Port)
        {
            tcpSocket = new TcpClient(Hostname, Port);
        }

        public bool IsConnected => tcpSocket.Connected;

        public string Login(string Username, string Password, int LoginTimeOutMs)
        {
            var timeOutMs = TimeOutMs;
            TimeOutMs = LoginTimeOutMs;
            var str = Read();
            if (!str.TrimEnd().EndsWith(":")) throw new Exception("Failed to connect : no login prompt");
            WriteLine(Username);
            str = string.Concat(str, Read());
            if (!str.TrimEnd().EndsWith(":")) throw new Exception("Failed to connect : no password prompt");
            WriteLine(Password);
            str = string.Concat(str, Read());
            TimeOutMs = timeOutMs;
            return str;
        }

        private void ParseTelnet(StringBuilder sb)
        {
            while (tcpSocket.Available > 0)
            {
                var num = tcpSocket.GetStream().ReadByte();
                var num1 = num;
                if (num1 != -1)
                {
                    if (num1 == 255)
                    {
                        var num2 = tcpSocket.GetStream().ReadByte();
                        if (num2 != -1)
                        {
                            var num3 = num2;
                            if (num3 - 251 <= 3)
                            {
                                var num4 = tcpSocket.GetStream().ReadByte();
                                if (num4 != -1)
                                {
                                    tcpSocket.GetStream().WriteByte(255);
                                    if (num4 != 3)
                                        tcpSocket.GetStream().WriteByte((byte)(num2 == 253 ? 252 : 254));
                                    else
                                        tcpSocket.GetStream().WriteByte((byte)(num2 == 253 ? 251 : 253));
                                    tcpSocket.GetStream().WriteByte((byte)num4);
                                }
                            }
                            else if (num3 == 255)
                            {
                                sb.Append(num2);
                            }
                        }
                    }
                    else
                    {
                        sb.Append((char)num);
                    }
                }
            }
        }

        public string Read()
        {
            string str;
            if (tcpSocket.Connected)
            {
                var stringBuilder = new StringBuilder();
                do
                {
                    ParseTelnet(stringBuilder);
                    Thread.Sleep(TimeOutMs);
                } while (tcpSocket.Available > 0);

                str = stringBuilder.ToString();
            }
            else
            {
                str = null;
            }

            return str;
        }

        public void SendFile(string localFilePath, string remoteFilePath, bool binary = false)
        {
            var base64String = "";
            if (!binary)
            {
                var str = File.ReadAllText(localFilePath);
                var chr = '\n';
                str = str.Replace(Environment.NewLine, chr.ToString());
                base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
            }
            else
            {
                base64String = Convert.ToBase64String(File.ReadAllBytes(localFilePath));
            }

            WriteLine(string.Concat("echo \"", base64String, "\" > ", remoteFilePath, ".base64"));
            Read();
            WriteLine(string.Concat("base64 -d ", remoteFilePath, ".base64 > ", remoteFilePath));
            Read();
            WriteLine(string.Concat("rm ", remoteFilePath, ".base64"));
            Read();
            Console.WriteLine(string.Concat("finished copy ", localFilePath, " -> ", remoteFilePath));
        }

        public void Write(string cmd)
        {
            if (tcpSocket.Connected)
            {
                var bytes = Encoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
                tcpSocket.GetStream().Write(bytes, 0, bytes.Length);
            }
        }

        public void WriteLine(string cmd)
        {
            Write(string.Concat(cmd, "\n"));
        }
    }
}