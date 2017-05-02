using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Krobotkin.Modules.Administration {
    class Blacklist : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateGroup("blacklist", bgp => {
                bgp.CreateCommand("print")
                .Do(async e => {
                    await e.Channel.SendIsTyping();
                    await e.Channel.SendMessage("+++++++++++++++++ CURRENT BLACKLIST +++++++++++++++++");
                    foreach (String word in Config.INSTANCE.Blacklist) {
                        await e.Channel.SendMessage(word);
                    }
                    await e.Channel.SendMessage("++++++++++++++++++ BLACKLIST ENDS +++++++++++++++++++");
                });
                bgp.CreateCommand("add")
                .Parameter("word")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        Config.INSTANCE.Blacklist.Add(e.GetArg("word"));
                        ModerationLog.LogToCabal($"User {e.User} added the word {e.Args[0]} to the blacklist.", e.Server);
                        Config.INSTANCE.Commit();
                    }
                });
                bgp.CreateCommand("remove")
                .Parameter("word")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        Config.INSTANCE.Blacklist.Remove(e.Args[0]);
                        ModerationLog.LogToCabal($"User {e.User} removed the word {e.Args[0]} from the blacklist.", e.Server);
                    }
                });
            });
        }
    }
}
