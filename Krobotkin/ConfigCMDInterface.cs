using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Krobotkin {
    public static class ConfigCMDInterface {
        public static void Tick() {
            var input = Console.ReadLine();
            if (input.StartsWith("info ")) {
                var inputParams = input.Split(' ');
                var serverid = ulong.Parse(inputParams[1]);

                var server = (from serv in DiscordClient.Servers where serv.Id == serverid select serv).First();

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
    }
}
