using System.Net;
using App;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    // Integration tests use the TCP/IP interface to test the server behaviour.
    public class IntegrationTests
    {
        private const int Port = 8080;
        private static readonly IPAddress LocalAddr = IPAddress.Parse("127.0.0.1");
        private const int Reps = 16;

        [Fact]
        public void First()
        {
            var server = new Server(_loggerFactory);
            server.Start(LocalAddr, Port);
            var client0 = new TestClient(LocalAddr, Port, _loggerFactory);
            var client1 = new TestClient(LocalAddr, Port, _loggerFactory);
            Assert.Null(client0.EnterRoom("room"));
            Assert.Null(client1.EnterRoom("room"));
            for (var i = 0; i < Reps; ++i)
            {
                client0.WriteLine($"Hello from client0 {i}");
                client1.WriteLine($"Hello from client1 {i}");
                Assert.Equal($"[room]client-1 says 'Hello from client1 {i}'", client0.ReadLine());
                Assert.Equal($"[room]client-0 says 'Hello from client0 {i}'", client1.ReadLine());
            }
            Assert.Null(client0.LeaveRoom());
            Assert.Null(client1.LeaveRoom());
            Assert.Null(client0.Exit());
            server.Stop();
            server.Join();
            Assert.Equal("[Error: Server is exiting]", client1.ReadLine());
        }
        
        public IntegrationTests(ITestOutputHelper output)
        {
            _loggerFactory = Logging.CreateFactory(new XUnitLoggingProvider(output));
        }

        private readonly ILoggerFactory _loggerFactory;
    }
}