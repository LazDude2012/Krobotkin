using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Krobotkin.Modules.Administration {
    class Purge : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("purge")
                .Alias("delet")
                .Alias("delete")
                .Description("Clears messages from a channel.")
                .Parameter("number", type: ParameterType.Required)
                .Parameter("user", ParameterType.Optional)
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        var purgemessages = await e.Channel.DownloadMessages(Int32.Parse(e.Args[0]) + 1);
                        if (e.GetArg("user") == "")
                            await e.Channel.DeleteMessages(purgemessages);
                        else {
                            foreach (Message msg in purgemessages) {
                                if (msg.User == e.Message.MentionedUsers.First()) await msg.Delete();
                            }
                        }
                    }
                    await e.Message.Delete();
                    ModerationLog.LogToPublic($"User {e.User.Name} purged {e.GetArg("number")} messages in #{e.Channel.Name}", e.Server);
                }
            );
        }
    }
}
