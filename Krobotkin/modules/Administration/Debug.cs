using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Krobotkin.Modules.Administration {
    class Debug : Module {
        public override async void ParseMessage(Channel channel, Message message) {
            if (message.Text.ToLower() == "debug init") { 
                if (Config.INSTANCE.GetPermissionLevel(message.User, message.Server) > 2) {
                    await channel.SendMessage("Debug information printed; config file initialized. All role permissions reset.");
                    foreach (Server serv in Krobotkin.DiscordClient.Servers) {
                        foreach (Role r in serv.Roles) {
                            Config.INSTANCE.roles.Add(new ConfigRole { name = $"{serv.Name} -> {r.Name}", role_id = r.Id, server_id = serv.Id, trust_level = 0 });
                        }
                    }
                }
            }
        }
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("quit")
                .Hide()
                .Do(e =>
                {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 2) {
                        _client.Disconnect();
                        throw new Exception();
                    }
                }
            );
            _client.GetService<CommandService>().CreateCommand("testwelcome")
                .Hide()
                .Parameter("user")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 2) {
                        WelcomeMessage.Display(e.Message.MentionedUsers.First());
                    }
                });
        }
    }
}
