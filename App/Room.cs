using System.Collections.Generic;

namespace App
{
    /*
     * Manages a room, namely the set of contained clients.
     * Must be thread-safe.
     */
    // FIXME
    public class Room
    {
        private readonly ISet<ConnectedClient> _clients = new HashSet<ConnectedClient>();

        public Room(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public void Enter(ConnectedClient client)
        {
            _clients.Add(client);
        }

        public void Leave(ConnectedClient client)
        {
            _clients.Remove(client);
        }

        public void Post(ConnectedClient client, string message)
        {
            var formattedMessage = $"[{Name}]{client.Name} says '{message}'";
            foreach (var receiver in _clients)
            {
                if (receiver != client)
                {
                    receiver.PostRoomMessage(formattedMessage, this);
                }
            }
        }
    }
}