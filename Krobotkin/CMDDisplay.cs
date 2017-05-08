using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrobotkinDiscord {
    public static class CMDDisplay {
        public static void Tick() {
            var input = Console.ReadLine();
            if (input.StartsWith("info ")) {
                var inputParams = input.Split(' ');
                var serverid = ulong.Parse(inputParams[1]);

                var server = (from serv in Program.DiscordClient.Servers where serv.Id == serverid select serv).First();

                Console.WriteLine($"Name: {server.Name}");
                Console.WriteLine("Roles:");
                foreach (Role role in server.Roles) {
                    Console.WriteLine($"  Role Name: {role.Name}");
                    Console.WriteLine($"  Role Id: {role.Id}");
                    Console.WriteLine($"  Role Colour: {role.Color}");
                    Console.WriteLine();
                }
                Console.WriteLine("Text Channels:");
                foreach (Channel channel in server.TextChannels) {
                    Console.WriteLine($"\t{channel.Name}: {channel.Id}");
                }
                Console.WriteLine();
                Console.WriteLine("Voice Channels:");
                foreach (Channel channel in server.VoiceChannels) {
                    Console.WriteLine($"\t{channel.Name}: {channel.Id}");
                }
            }
        }

        public static async void OnServerAvailable(object sender, ServerEventArgs e) {
            Console.WriteLine($"Joined Server {e.Server.Name}: {e.Server.Id}");
            try {
                Channel general = (from channel in Config.INSTANCE.primaryChannels
                                   where channel.server_id == e.Server.Id
                                   select Program.DiscordClient.GetChannel(channel.channel_id)
                                  ).First();
                await general.SendMessage($"Krobotkin {Program.VERSION} initialised.");
            } catch (Exception) {
                Console.WriteLine($"Failed to send greeting to {e.Server.Name}");
            }
        }

        public static void Start() {
            Console.WriteLine("CONNECTED");
            Console.WriteLine("Commands:");
            Console.WriteLine("info <serverid> - view info about server");
            while (true) {
                Tick();
            }
        }
    }
}
