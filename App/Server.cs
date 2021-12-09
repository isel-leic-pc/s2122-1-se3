using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace App
{
    /*
     * Represents a server, listening and handling connections.
     * Must be thread safe, namely multiple threads can call Start, Stop and Join.
     */
    public class Server
    {
        public enum Status
        {
            NotStarted,
            Starting,
            Started,
            Ending,
            Ended
        }

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private readonly RoomSet _rooms = new RoomSet();

        private Thread? _acceptThread;
        private Status _status = Status.NotStarted;
        private TcpListener? _listener;
        private int _nextClientId;

        public Server(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<Server>();
        }

        public Status State => _status;

        public void Start(IPAddress address, int port)
        {
            if (_status != Status.NotStarted)
            {
                // Can be started at most one once
                throw new Exception("Server has already started");
            }

            _status = Status.Starting;
            _logger.LogInformation("Starting");
            _listener = new TcpListener(address, port);
            _listener.Start();
            _acceptThread = new Thread(() => AcceptLoop(_listener));
            _acceptThread.Start();
            _status = Status.Started;
        }

        public void Stop()
        {
            if (_status == Status.NotStarted)
            {
                _logger.LogError("Server has not started");
                throw new Exception("Server has not started");
            }

            // FIXME what if it is starting?
            if (_listener == null)
            {
                _logger.LogError("Unexpected state: listener is not set");
                throw new Exception("Unexpected state");
            }

            _logger.LogInformation("Changing server status and stopping the listener");
            _status = Status.Ending;

            _listener.Stop();
            _logger.LogInformation("Listener stopped");
        }

        public void Join()
        {
            if (_status == Status.NotStarted)
            {
                _logger.LogError("Server has not started");
                throw new Exception("Server has not started");
            }

            // FIXME what if it is starting?
            if (_acceptThread == null)
            {
                _logger.LogError("Unexpected state: acceptThread is not set");
                throw new Exception("Unexpected state");
            }

            _acceptThread.Join();
        }

        private void AcceptLoop(TcpListener listener)
        {
            _logger.LogInformation("Accept thread started");
            var clients = new Collection<ConnectedClient>();
            _status = Status.Started;
            while (_status == Status.Started)
            {
                try
                {
                    _logger.LogInformation("Waiting for client");
                    // Because the blocking call may not react to the listener Close
                    // See https://github.com/dotnet/runtime/issues/24513
                    // TODO define and enforce a maximum number of connected clients
                    var tcpClient = listener.AcceptTcpClientAsync().Result;

                    var clientName = $"client-{_nextClientId++}";
                    _logger.LogInformation("New client accepted '{}'", clientName);
                    var client = new ConnectedClient(clientName, tcpClient, _rooms, _loggerFactory);
                    
                    // FIXME remove clients from this collection when they exit
                    clients.Add(client);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(
                        "Exception caught '{}', which may happen when the listener is closed, continuing...",
                        e.Message);
                    // continuing...
                }
            }

            _logger.LogInformation("Waiting for clients to end, before ending accept loop");
            foreach (var client in clients)
            {
                client.Exit();
                client.Join();
            }

            _logger.LogInformation("Accept thread ending");
            _status = Status.Ended;
        }
    }
}