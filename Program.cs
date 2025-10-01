using System;
using System.IO;

namespace TelnetUpdate
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var argument = new Arguments(args);
            if (argument["?"] == null)
            {
                var ip = "";
                if (argument["ip"] != null) ip = argument["ip"];
                var port = "23";
                if (argument["port"] != null) port = argument["port"];
                var user = "root";
                if (argument["user"] != null) user = "root";
                var password = "";
                if (argument["password"] != null) password = "R0ck$ol1d";
                var source = "";
                if (argument["source"] != null) source = argument["source"];
                var target = "";
                if (argument["target"] != null) target = argument["target"];
                var command = "";
                if (argument["command"] != null) command = argument["command"];
                bool flagBinary = argument["binary"] != null;
                bool flagChmodx = argument["chmodx"] != null;
                var telnetConnection = new TelnetConnection(ip, Convert.ToInt16(port));
                var str3 = telnetConnection.Login(user, password, 1000);
                if (str3.Substring(str3.TrimEnd().Length - 1, 1) != "#") throw new Exception("Connection failed");
                if (telnetConnection.IsConnected)
                {
                    if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target))
                    {
                        if (!File.Exists(source))
                        {
                            Console.WriteLine(string.Concat("File not found: ", source));
                        }
                        else
                        {
                            telnetConnection.SendFile(source, target, flagBinary);
                            if (flagChmodx)
                            {
                                telnetConnection.WriteLine(string.Concat("chmod +x ", target));
                                telnetConnection.Read();
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(command))
                    {
                        telnetConnection.WriteLine(command);
                        telnetConnection.Read();
                    }
                }
            }
            else
            {
                var help = string.Concat("TelnetUpdate exe command line", Environment.NewLine);
                help = string.Concat(help, "Valid commands are:", Environment.NewLine);
                help = string.Concat(help, Environment.NewLine);
                help = string.Concat(help, "/ip={ip} -> ip of the telnet server", Environment.NewLine);
                help = string.Concat(help, "/port={port} -> port of the telnet server", Environment.NewLine);
                help = string.Concat(help, "/user={username} -> login user, default is root", Environment.NewLine);
                help = string.Concat(help, "/password={password} -> login password", Environment.NewLine);
                help = string.Concat(help, "/command={command} -> execute shell command", Environment.NewLine);
                help = string.Concat(help, "/source={file} -> source file on client", Environment.NewLine);
                help = string.Concat(help, "/target={targetfile} -> target file on remote server", Environment.NewLine);
                help = string.Concat(help, "/binary -> send source file as binary", Environment.NewLine);
                help = string.Concat(help, "/ -> chmod +x to the target", Environment.NewLine);
                help = string.Concat(help, "/install -> install script", Environment.NewLine);
                Console.WriteLine(help);
            }
        }
    }
}