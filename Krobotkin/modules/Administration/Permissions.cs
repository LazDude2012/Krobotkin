using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KrobotkinDiscord.Modules.Administration {
    class Permissions : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateGroup("permissions", pgp => {
                pgp.CreateCommand("check")
                    .Description("Informs a user of their permission level.")
                    .Do(e => {
                        int perms = Config.INSTANCE.GetPermissionLevel(e.User, e.Server);
                        switch (perms) {
                            case -1:
                                e.User.SendMessage("You should be sleeping");
                                break;
                            case 0:
                                e.Channel.SendMessage("You have :couch: NORMAL USER :couch: permissions.");
                                break;
                            case 1:
                                e.Channel.SendMessage("You have :shield: TRUSTED USER :shield: permissions.");
                                break;
                            case 2:
                                e.Channel.SendMessage("You have :zap: MODERATOR :zap: permissions.");
                                break;
                            case 3:
                                e.Channel.SendMessage("You are a :tools: :zap: BOT WIZARD :zap: :tools:.");
                                break;
                        }
                    }
                );
                pgp.CreateCommand("set")
                    .Parameter("role")
                    .Parameter("level")
                    .Description("Sets a role's permission level.")
                    .Do(e => {
                        int level = Int32.Parse(e.GetArg("level"));
                        int userLevel = Config.INSTANCE.GetPermissionLevel(e.User, e.Server);

                        // user permission level check
                        if (userLevel < 2) {
                            e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                            return;
                        }

                        if (userLevel <= level) {
                            e.Channel.SendMessage($"You cannot alter permission levels at or above your own level ({userLevel}).");
                            return;
                        }

                        // check that role exists
                        String roleName = e.GetArg("role");
                        Role r = e.Server.FindRoles(roleName).FirstOrDefault();
                        if (r == null) {
                            e.Channel.SendMessage($"Role `{roleName}` does not exist.");
                            return;
                        }

                        // find role and set permission level
                        bool roleFound = false;
                        foreach (ConfigRole cr in Config.INSTANCE.roles) {
                            if (cr.role_id == r.Id) {
                                cr.trust_level = level;
                                roleFound = true;
                                break;
                            }
                        }

                        // if role doesn't exist, create it and set permission level
                        if (!roleFound) {
                            ConfigRole newRole = new ConfigRole {
                                server_id = e.Server.Id,
                                role_id = r.Id,
                                trust_level = level
                            };
                            Config.INSTANCE.roles.Add(newRole);
                        }

                        // save changes
                        e.Channel.SendMessage($"Permission level for role `{roleName}` set to {level}.");
                        Config.INSTANCE.Commit();
                    }
                );
            });
        }
    }
}
