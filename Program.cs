using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading; 

namespace TelnetUpdate
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var commandLine = new Arguments(args);

            if (commandLine["?"] != null)
            {
                var console = "TelnetUpdate exe command line" + Environment.NewLine;
                console += "Valid commands are:" + Environment.NewLine;
                console += Environment.NewLine;
                console += "/ip={ip} -> ip of the telnet server" + Environment.NewLine;
                console += "/port={port} -> port of the telnet server" + Environment.NewLine;
                console += "/user={username} -> login user, default is root" + Environment.NewLine;
                console += "/password={password} -> login password" + Environment.NewLine;
                console += "/command={command} -> execute shell command" + Environment.NewLine;
                console += "/source={file} -> source file on client" + Environment.NewLine;
                console += "/target={targetfile} -> target file on remote server" + Environment.NewLine;
                console += "/binary -> send source file as binary" + Environment.NewLine;
                console += "/chmodx -> chmod +x to the target" + Environment.NewLine;

                Console.WriteLine(console); 
                return;
            }

            var ip = "";
            if (commandLine["ip"] != null)
                ip = commandLine["ip"];

            var port = "23";
            if (commandLine["port"] != null)
                port = commandLine["port"];

            var user = "root";
            if (commandLine["user"] != null)
                user = commandLine["user"];

            var password = "";
            if (commandLine["password"] != null)
                password = commandLine["password"];

            var source = "";
            if (commandLine["source"] != null)
                source = commandLine["source"];

            var target = "";
            if (commandLine["target"] != null)
                target = commandLine["target"];

            var command = "";
            if (commandLine["command"] != null)
                command = commandLine["command"];

            var binary = false;
            if (commandLine["binary"] != null)
                binary = true;

            var chmodx = false;
            if (commandLine["chmodx"] != null)
                chmodx = true;

            var tc = new TelnetConnection(ip, Convert.ToInt16(port));
            var s = tc.Login(user, password, 100);
            //Console.Write(s);

            // server output should end with "$" or ">", otherwise the connection failed
            var prompt = s.TrimEnd();
            prompt = s.Substring(prompt.Length - 1, 1);
            if (prompt != "#")
                throw new Exception("Connection failed");

            if (tc.IsConnected)
            {
                if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target))
                {
                    if (File.Exists(source))
                    {
                        tc.SendFile(source, target, binary);
                        if (chmodx) tc.WriteLine("chmod +x " + target);
                    }
                    else
                    {
                        Console.WriteLine("File not found: " + source);
                    }
                }

                if (!string.IsNullOrEmpty(command)) tc.WriteLine(command);
            }
        }
    }

    internal enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    internal enum Options
    {
        SGA = 3
    }

    internal class TelnetConnection
    {
        private readonly TcpClient tcpSocket;

        private int TimeOutMs = 100;

        public TelnetConnection(string Hostname, int Port)
        {
            tcpSocket = new TcpClient(Hostname, Port);
        }

        public bool IsConnected => tcpSocket.Connected;

        public string Login(string Username, string Password, int LoginTimeOutMs)
        {
            var oldTimeOutMs = TimeOutMs;
            TimeOutMs = LoginTimeOutMs;
            var s = Read();
            if (!s.TrimEnd().EndsWith(":"))
                throw new Exception("Failed to connect : no login prompt");
            WriteLine(Username);

            s += Read();
            if (!s.TrimEnd().EndsWith(":"))
                throw new Exception("Failed to connect : no password prompt");
            WriteLine(Password);

            s += Read();
            TimeOutMs = oldTimeOutMs;
            return s;
        }

        public void WriteLine(string cmd)
        {
            Write(cmd + "\n");
        }

        public void Write(string cmd)
        {
            if (!tcpSocket.Connected) return;
            var buf = Encoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
            tcpSocket.GetStream().Write(buf, 0, buf.Length);
        }

        public string Read()
        {
            if (!tcpSocket.Connected) return null;
            var sb = new StringBuilder();
            do
            {
                ParseTelnet(sb);
                Thread.Sleep(TimeOutMs);
            } while (tcpSocket.Available > 0);

            return sb.ToString();
        }

        private void ParseTelnet(StringBuilder sb)
        {
            while (tcpSocket.Available > 0)
            {
                var input = tcpSocket.GetStream().ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int) Verbs.IAC:
                        // interpret as command
                        var inputverb = tcpSocket.GetStream().ReadByte();
                        if (inputverb == -1) break;
                        switch (inputverb)
                        {
                            case (int) Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int) Verbs.DO:
                            case (int) Verbs.DONT:
                            case (int) Verbs.WILL:
                            case (int) Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                var inputoption = tcpSocket.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                tcpSocket.GetStream().WriteByte((byte) Verbs.IAC);
                                if (inputoption == (int) Options.SGA)
                                    tcpSocket.GetStream().WriteByte(inputverb == (int) Verbs.DO
                                        ? (byte) Verbs.WILL
                                        : (byte) Verbs.DO);
                                else
                                    tcpSocket.GetStream().WriteByte(inputverb == (int) Verbs.DO
                                        ? (byte) Verbs.WONT
                                        : (byte) Verbs.DONT);
                                tcpSocket.GetStream().WriteByte((byte) inputoption);
                                break;
                        }

                        break;
                    default:
                        sb.Append((char) input);
                        break;
                }
            }
        }

        public void SendFile(string localFilePath, string remoteFilePath, bool binary = false)
        {
            var base64 = "";
            if (binary)
            {
                var fileContent = File.ReadAllBytes(localFilePath);
                base64 = Convert.ToBase64String(fileContent);
            }
            else
            {
                var fileContent = File.ReadAllText(localFilePath);
                fileContent = fileContent.Replace(Environment.NewLine, '\n'.ToString());

                base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileContent));
            }

            WriteLine($"echo \"{base64}\" > {remoteFilePath + ".base64"}");
            Read();
            WriteLine($"base64 -d {remoteFilePath + ".base64"} > {remoteFilePath}");
            Read();
            WriteLine($"rm {remoteFilePath + ".base64"}");
            Read(); 
            Console.WriteLine("finished copy " + localFilePath +  " -> " + remoteFilePath);
        }
    }
}