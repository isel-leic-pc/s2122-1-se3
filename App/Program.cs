using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace App
{
    public class Program
    {
        static void Main()
        {

            var loggerFactory = Logging.CreateFactory();
            var logger = loggerFactory.CreateLogger<Program>();
            
            logger.LogInformation("Starting program");

            var server = new Server(loggerFactory);
            var port = 8080;
            var localAddr = IPAddress.Parse("127.0.0.1");
            server.Start(localAddr, port);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                logger.LogInformation("Stopping the server");
                server.Stop();
            };
            
            server.Join();
        }
    }
}