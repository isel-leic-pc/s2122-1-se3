using System.Collections.Generic;

namespace App
{
    /*
     * Manages a set of rooms, namely the creation and retrieval.
     * Must be thread-safe.
     */
    // FIXME
    public class RoomSet
    {
        private readonly IDictionary<string, Room> _rooms = new Dictionary<string, Room>();
        
        public Room GetOrCreateRoom(string name)
        {
            if (_rooms.ContainsKey(name))
            {
                return _rooms[name];
            }

            var room = new Room(name);
            _rooms[name] = room;
            return room;
        }
    }
}