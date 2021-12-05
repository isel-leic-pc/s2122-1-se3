namespace App
{
    /*
     * Closed class hierarchy to parse and represent lines sent by a remote client.
     */
    public abstract class Line
    {
        private Line()
        {
            // to make the hierarchy closed
        }

        public class Message : Line
        {
            public string Value { get; }

            internal Message(string value)
            {
                Value = value;
            }
        }
        
        public class EnterRoomCommand : Line
        {
            public string Name { get; }

            internal EnterRoomCommand(string name)
            {
                Name = name;
            }
        }
        
        public class LeaveRoomCommand : Line
        {
        }
        
        public class ExitCommand : Line
        {
        }

        public class InvalidLine : Line
        {
            public string Reason { get; }

            internal InvalidLine(string reason)
            {
                Reason = reason;
            }
        }
        
        public static Line Parse(string line)
        {
            if (!line.StartsWith("/"))
            {
                return new Message(line);
            }
            
            var parts = line.Split();
            return parts[0] switch
            {
                "/enter" => ParseEnterRoom(parts),
                "/leave" => ParseLeaveRoom(parts),
                "/exit" => ParseExit(parts),
                _ => new InvalidLine("Unknown command")
            };
        }

        private static Line ParseEnterRoom(string[] parts)
        {
            if (parts.Length != 2)
            {
                return new InvalidLine("/enter command requires exactly one argument");
            }
            return new EnterRoomCommand(parts[1]);
        }
        
        private static Line ParseLeaveRoom(string[] parts)
        {
            if (parts.Length != 1)
            {
                return new InvalidLine("/leave command does not have arguments");
            }
            return new LeaveRoomCommand();
        }
        
        private static Line ParseExit(string[] parts)
        {
            if (parts.Length != 1)
            {
                return new InvalidLine("/exit command does not have arguments");
            }
            return new ExitCommand();
        }
    }
}