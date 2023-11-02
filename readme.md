# TelnetUpdate.exe

The `TelnetUpdate` program is a simple Telnet client in C# that allows you to establish a connection to a Telnet server, 
execute shell commands, and copy files to a remote server.

## Usage

To run the program, use command-line arguments. Here are the available options:

- `/ip={ip}`: IP address of the Telnet server (required).
- `/port={port}`: Port of the Telnet server (default: 23).
- `/user={username}`: Username for login (default: root).
- `/password={password}`: Password for login.
- `/command={command}`: Shell command to be executed on the remote server.
- `/source={file}`: Path to the source file on the client.
- `/target={targetfile}`: Target on the remote server.
- `/binary`: Send the source file as a binary.
- `/chmodx`: Set as executable (chmod +x) on the remote server.

Example:

```bash
TelnetUpdate.exe /ip=192.168.1.100 /user=myuser /password=mypassword /source=localfile.txt /target=remotefile.txt
TelnetUpdate.exe /ip=192.168.174.111 /port=5230 /user=root /password=mypassword /source="C:\setupjson.cgi" /target="/usr/local/www/htdocs/setupjson.cgi" /chmodx
TelnetUpdate.exe /ip=192.168.1.100 /user=myuser /password=mypassword /command=reboot
```

## Dependencies

The program uses the following C# libraries:

- `System`
- `System.IO`
- `System.Net.Sockets`
- `System.Text`
- `System.Threading`

## License

This program is under the MIT license.

## Author

Diagraph GmbH