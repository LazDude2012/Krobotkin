using System;
using System.Linq;
using Discord;
using Discord.Commands;
using System.Collections.Generic;

namespace KrobotkinDiscord.Modules.Administration {
    class UserManagement : Module {
        Dictionary<User, List<Role>> lobbiedUsers = new Dictionary<User, List<Role>>();

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
                        ModerationLog.LogToPublic($"User {e.User.Name} kicked user(s) {usersKicked}", e.Server);
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
                        ModerationLog.LogToPublic($"User {e.User.Name} banned user(s) {usersBanned}", e.Server);
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

            _client.GetService<CommandService>().CreateCommand("verify")
                .Alias("timeout")
                .Description("Gives a user the `verified` role if it exists on the server.")
                .Parameter("user")
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    Role role = e.Server.FindRoles("verified", true).FirstOrDefault();
                    if (role == null) {
                        e.Channel.SendMessage("Verify command requires a `verified` server role.");
                        return;
                    }

                    // get user
                    User user = e.Message.MentionedUsers.FirstOrDefault();
                    if (user == null) {
                        e.Channel.SendMessage("Command usage: !verify @username");
                        return;
                    }

                    // check if already verified
                    if (user.HasRole(role)) {
                        e.Channel.SendMessage("User is already verified.");
                        return;
                    }

                    // give user role
                    user.AddRoles(role);
                });

                    _client.GetService<CommandService>().CreateCommand("lobby")
                .Alias("timeout")
                .Description("Removes a user's roles, which can be reassigned with !unlobby.")
                .Parameter("user")
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    // get user
                    User user = e.Message.MentionedUsers.FirstOrDefault();
                    if (user == null) {
                        e.Channel.SendMessage("Command usage: !lobby @username");
                        return;
                    }

                    // compare permission level
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) <= Config.INSTANCE.GetPermissionLevel(user, e.Server)) {
                        e.Channel.SendMessage("Cannot lobby someone with equal/higher permission level.");
                        return;
                    }

                    // check if already lobbied
                    if (lobbiedUsers.ContainsKey(user)) {
                        e.Channel.SendMessage("User is already lobbied.");
                        return;
                    }

                    // lobby user (remove roles)
                    List<Role> removedRoles = new List<Role>();
                    foreach (Role r in user.Roles) {
                        removedRoles.Add(r);
                    }
                    user.RemoveRoles(removedRoles.ToArray());
                    lobbiedUsers[user] = removedRoles;
                });

            _client.GetService<CommandService>().CreateCommand("unlobby")
                .Description("Restores a user's roles.")
                .Parameter("user")
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    // get user
                    User user = e.Message.MentionedUsers.FirstOrDefault();
                    if (user == null) {
                        e.Channel.SendMessage("Command usage: !unlobby @username");
                        return;
                    }

                    // compare permission level
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) <= Config.INSTANCE.GetPermissionLevel(user, e.Server)) {
                        e.Channel.SendMessage("Cannot unlobby someone with equal/higher permission level.");
                        return;
                    }

                    // check if already lobbied
                    if (!lobbiedUsers.ContainsKey(user)) {
                        e.Channel.SendMessage("User is not lobbied.");
                        return;
                    }

                    // unlobby user (restore roles)
                    user.AddRoles(lobbiedUsers[user].ToArray());
                    lobbiedUsers.Remove(user);
                });
        }
    }
}
