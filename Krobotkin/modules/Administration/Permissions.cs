using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Krobotkin.Modules.Administration {
    class Permissions : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateGroup("permissions", pgp => {
                pgp.CreateCommand("check")
                    .Description("Informs a user of their permission level.")
                    .Do(e => {
                        int perms = Config.INSTANCE.GetPermissionLevel(e.User, e.Server);
                        switch (perms) {
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
                    .Description("Sets a user's permission level.")
                    .Do(e => {
                        if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > Int32.Parse(e.GetArg("level"))) {
                            Role r = e.Server.FindRoles(e.GetArg("role")).First();
                            foreach (ConfigRole cr in Config.INSTANCE.roles) {
                                if (cr.role_id == r.Id) cr.trust_level = Int32.Parse(e.GetArg("level"));
                            }
                        }
                        Config.INSTANCE.Commit();
                    }
                );
            });
        }
    }
}
