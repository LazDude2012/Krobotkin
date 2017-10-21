using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KrobotkinDiscord.Modules.Administration {
    class Purge : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("purge")
                .Alias("delet")
                .Alias("delete")
                .Alias("settler-colonize")
                .Description("Clears messages from a channel.")
                .Parameter("number", type: ParameterType.Required)
                .Parameter("user", ParameterType.Optional)
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        var purgemessages = await e.Channel.DownloadMessages(Int32.Parse(e.Args[0]) + 1);
                        if (e.GetArg("user") == "")
                            // Delete any messages
                            await e.Channel.DeleteMessages(purgemessages);
                        else {
                            // Delete messages from specified user
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
