using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMinigamePlugin
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Multiplayer Minigame Plugin";

        private readonly DalamudPluginInterface pluginInterface;
        private TcpListener? server;
        private bool isHosting = false;
        private readonly object hostingLock = new object();

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            this.pluginInterface.CommandManager.AddHandler("/mminigame", new CommandInfo(OnCommand)
            {
                HelpMessage = "Usage: /mminigame host | stop"
            });
        }

        public void Dispose()
        {
            this.pluginInterface.CommandManager.RemoveHandler("/mminigame");
            StopHosting();
        }

        private void OnCommand(string command, string args)
        {
            switch (args.Trim().ToLower())
            {
                case "host":
                    StartHosting();
                    break;
                case "stop":
                    StopHosting();
                    break;
                default:
                    this.pluginInterface.Framework.Gui.Chat.Print("Usage: /mminigame host | stop");
                    break;
            }
        }

        private async void StartHosting()
        {
            lock (hostingLock)
            {
                if (isHosting)
                {
                    this.pluginInterface.Framework.Gui.Chat.Print("Already hosting.");
                    return;
                }

                isHosting = true;
            }

            try
            {
                // Listen on all interfaces at port 9000.
                server = new TcpListener(IPAddress.Any, 9000);
                server.Start();
                this.pluginInterface.Framework.Gui.Chat.Print("Hosting multiplayer minigame on port 9000.");

                await Task.Run(async () =>
                {
                    while (isHosting)
                    {
                        try
                        {
                            var client = await server.AcceptTcpClientAsync();
                            HandleClient(client);
                        }
                        catch (SocketException)
                        {
                            // Likely caused by stopping the server.
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                this.pluginInterface.Framework.Gui.Chat.Print($"Error starting server: {ex.Message}");
                lock (hostingLock)
                {
                    isHosting = false;
                }
            }
        }

        private async void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var networkStream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // Log or process the received message.
                    this.pluginInterface.Framework.Gui.Chat.Print($"Received: {message}");

                    // Here you can integrate your game logic (for ludo, poker, blackjack, etc.)
                    // For now, simply echo the message back.
                    byte[] response = Encoding.UTF8.GetBytes("Message received.");
                    await networkStream.WriteAsync(response, 0, response.Length);
                }
            }
            catch (Exception ex)
            {
                this.pluginInterface.Framework.Gui.Chat.Print($"Error handling client: {ex.Message}");
            }
        }

        private void StopHosting()
        {
            lock (hostingLock)
            {
                if (!isHosting) return;
                isHosting = false;
            }

            server?.Stop();
            this.pluginInterface.Framework.Gui.Chat.Print("Stopped hosting multiplayer minigame.");
        }
    }
}