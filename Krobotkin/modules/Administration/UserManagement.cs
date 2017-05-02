using System;
using System.Linq;
using Discord;
using Discord.Commands;

namespace Krobotkin.modules.Administration {
    class UserManagement : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("kick")
                .Description("Kicks a user.")
                .Parameter("user")
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        String usersKicked = "";
                        foreach (User user in e.Message.MentionedUsers) {
                            if (!user.IsBot) {
                                usersKicked += (user.Name + " ");
                                await user.Kick();
                            }
                        }
                        ModerationLog.LogToCabal($"User {e.User.Name} kicked user(s) {usersKicked}", e.Server);
                    }
                }
            );

            _client.GetService<CommandService>().CreateCommand("ban")
                .Description("Bans a user.")
                .Parameter("user")
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        String usersBanned = "";
                        foreach (User user in e.Message.MentionedUsers) {
                            if (!user.IsBot) {
                                usersBanned += (user.Name + " ");
                                await e.Server.Ban(user, 3);
                            }
                        }
                        ModerationLog.LogToCabal($"User {e.User.Name} banned user(s) {usersBanned}", e.Server);
                    }
                }
            );

            _client.GetService<CommandService>().CreateCommand("forcenick")
                .Alias("fn")
                .Description("Forces a user's nickname to be changed.")
                .Parameter("user")
                .Parameter("nick")
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        User user = e.Server.FindUsers(e.GetArg("user")).First();
                        await user.Edit(nickname: e.GetArg("nick"));
                    }
                }
            );
        }
    }
}
