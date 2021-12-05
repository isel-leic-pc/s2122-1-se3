using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using App;
using Microsoft.Extensions.Logging;

namespace Tests
{
    public class TestClient
    {
        private readonly ILogger _logger ;
        private readonly TcpClient _tcpClient;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        public TestClient(IPAddress address, int port, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TestClient>();
            _tcpClient = new TcpClient();
            _tcpClient.Connect(address, port);
            var stream = _tcpClient.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };
            _logger.LogInformation("TestClient connected");
        }

        public string? EnterRoom(string room)
        {
            _writer.WriteLine($"/enter {room}");
            return WaitForOk();
        }
        
        public string? LeaveRoom()
        {
            _writer.WriteLine("/leave");
            return WaitForOk();
        }
        
        public string? Exit()
        {
            _writer.WriteLine("/exit");
            return WaitForOk();
        }

        public void WriteLine(string s)
        {
            _writer.WriteLine(s);
        }
        
        public string ReadLine()
        {
            return _reader.ReadLine() ?? throw new Exception("Reader stream is closed");
        }

        public void Close()
        {
            _tcpClient.Close();
        }

        private string? WaitForOk()
        {
            _logger.LogInformation("TestClient reading line");
            var res = _reader.ReadLine();
            _logger.LogInformation("TestClient read line {}", res);
            if (res == null)
            {
                return "ReadLine returns null";
            }
            return res.Equals("[OK]") ? null : res;
        }

    }
}