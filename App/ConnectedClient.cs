using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace App
{
    /*
     * Represents a connected client.
     * Uses two threads:
     *     - A main thread that processes control messages sent to this client, which can be
     *         - A message delivered to the room where the client is.
     *         - A line sent by the remote client.
     *         - An indication the remote client stream has ended.
     *         - An indication to stop handling this client.
     *       This thread should only block for new control messages.
     *     - An auxiliary thread blocked waiting for input from the remote client.
     * 
     * Some of its methods may be used by multiple threads.
     */
    // FIXME race between leaving a room and receiving messages from that room.
    public class ConnectedClient
    {
        private readonly ILogger _logger;

        private readonly TcpClient _tcpClient;
        private readonly RoomSet _rooms;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly Thread _mainThread;

        private readonly BlockingCollection<ControlMessage> _controlMessageQueue = new BlockingCollection<ControlMessage>();

        private Room? _currentRoom;
        private bool _exiting;

        public string Name { get; }

        public ConnectedClient(string name, TcpClient tcpClient, RoomSet rooms, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConnectedClient>();
            Name = name;
            _tcpClient = tcpClient;
            _rooms = rooms;
            tcpClient.NoDelay = true;
            var networkStream = tcpClient.GetStream();
            _reader = new StreamReader(networkStream, Encoding.UTF8);
            _writer = new StreamWriter(networkStream, Encoding.UTF8)
            {
                AutoFlush = true
            };

            _mainThread = new Thread(MainLoop);
            _mainThread.Start();
        }

        // Sends a message to the client
        public void PostRoomMessage(string message, Room sender)
        {
            _controlMessageQueue.Add(new ControlMessage.RoomMessage(message, sender));
        }

        // Instructs the client to exit
        public void Exit()
        {
            _controlMessageQueue.Add(new ControlMessage.Stop());
        }

        // Synchronizes with the client termination
        public void Join()
        {
            _mainThread.Join();
        }

        private void MainLoop()
        {
            Thread? readThread = null;
            try
            {
                // Create a child thread to block on the socket read
                readThread = new Thread(RemoteReadLoop);
                readThread.Start();
                while (!_exiting)
                {
                    try
                    {
                        var controlMessage = _controlMessageQueue.Take();
                        switch (controlMessage)
                        {
                            case ControlMessage.RoomMessage roomMessage:
                                WriteToRemote(roomMessage.Value);
                                break;
                            case ControlMessage.RemoteLine remoteLine:
                                ExecuteCommand(remoteLine.Value);
                                break;
                            case ControlMessage.RemoteInputEnded:
                                ClientExit();
                                break;
                            case ControlMessage.Stop:
                                ServerExit();
                                break;
                            default:
                                _logger.LogWarning("Unknown message {}, ignoring it", controlMessage);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Unexpected exception while handling message: {}, ending connection", 
                            e.Message);
                        _exiting = true;
                    }
                }
            }
            finally
            {
                _currentRoom?.Leave(this);
                _tcpClient.Close();
                readThread?.Join();
                _logger.LogInformation("Exiting MainLoop");
            }
        }

        private void RemoteReadLoop()
        {
            try
            {
                while (!_exiting)
                {
                    var line = _reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    _controlMessageQueue.Add(new ControlMessage.RemoteLine(line));
                }
            }
            catch (Exception e)
            {
                // Unexpected exception, log and exit
                _logger.LogError("Exception while waiting for connection read: {}", e.Message);
            }
            finally
            {
                if (!_exiting)
                {
                    _controlMessageQueue.Add(new ControlMessage.RemoteInputEnded());
                }
            }

            _logger.LogInformation("Exiting ReadLoop");
        }


        private void WriteToRemote(string line)
        {
            _writer.WriteLine(line);
        }

        private void WriteErrorToRemote(string line) => WriteToRemote($"[Error: {line}]");
        private void WriteOkToRemote() => WriteToRemote("[OK]");

        private void ExecuteCommand(string lineText)
        {
            Line line = Line.Parse(lineText);

            switch (line)
            {
                case Line.InvalidLine invalidLine:
                    WriteErrorToRemote(invalidLine.Reason);
                    break;
                case Line.Message message:
                    PostMessageToRoom(message);
                    break;
                case Line.EnterRoomCommand enterRoomCommand:
                    EnterRoom(enterRoomCommand);
                    break;
                case Line.LeaveRoomCommand:
                    LeaveRoom();
                    break;
                case Line.ExitCommand:
                    ClientExit();
                    break;
                default:
                    WriteErrorToRemote("unable to process line");
                    break;
            }
        }

        private void PostMessageToRoom(Line.Message message)
        {
            if (_currentRoom == null)
            {
                WriteErrorToRemote("Need to be inside a room to post a message");
            }
            else
            {
                _currentRoom.Post(this, message.Value);
            }
        }

        private void EnterRoom(Line.EnterRoomCommand enterRoomCommand)
        {
            _currentRoom?.Leave(this);

            _currentRoom = _rooms.GetOrCreateRoom(enterRoomCommand.Name);
            _currentRoom.Enter(this);
            WriteOkToRemote();
        }

        private void LeaveRoom()
        {
            if (_currentRoom == null)
            {
                WriteErrorToRemote("There is no room to leave from");
            }
            else
            {
                _currentRoom.Leave(this);
                _currentRoom = null;
                WriteOkToRemote();
            }
        }

        private void ClientExit()
        {
            _currentRoom?.Leave(this);

            _exiting = true;
            WriteOkToRemote();
        }

        private void ServerExit()
        {
            _currentRoom?.Leave(this);
            _exiting = true;
            WriteErrorToRemote("Server is exiting");
        }

        private abstract class ControlMessage
        {
            private ControlMessage()
            {
                // to make the hierarchy closed
            }

            // A message sent by to a room
            public class RoomMessage : ControlMessage
            {
                public Room Sender { get; }
                public string Value { get; }

                public RoomMessage(string value, Room sender)
                {
                    Value = value;
                    Sender = sender;
                }
            }

            // A line sent by the remote client.
            public class RemoteLine : ControlMessage
            {
                public string Value { get; }

                public RemoteLine(string value)
                {
                    Value = value;
                }
            }

            // The information that the remote client stream has ended, probably because the 
            // socket was closed.
            public class RemoteInputEnded : ControlMessage
            {
            }

            // An instruction to stop handling this remote client
            public class Stop : ControlMessage
            {
            }
        }
    }
}