using Discord;
using Discord.Commands;
using KrobotkinDiscord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Krobotkin.Modules.Administration {
    class Sleep : Module {
        bool isUserSleeped(User user) {
            var matchedUsers = (from u in Config.INSTANCE.sleepedUsers where u.user_id == user.Id select u);

            return matchedUsers.Count() == 1;
        }

        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("sleep")
                .Alias("slep")
                .Alias("mute")
                .Description("Sleeps a user.")
                .Parameter("user")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        var u = e.Message.MentionedUsers.First();
                        if (!isUserSleeped(u)) {
                            Config.INSTANCE.sleepedUsers.Add(new ConfigUser() { user_id = u.Id});
                            Config.INSTANCE.Commit();
                            e.Channel.SendMessage(u.Mention + " has gone to sleep, just like that.");
                            u.Edit(true, true);
                        } else {
                            e.Channel.SendMessage(u + " is already asleep");
                        }
                    }
                }
            );
            _client.GetService<CommandService>().CreateCommand("unsleep")
                .Alias("unslep")
                .Alias("unmute")
                .Description("Unsleeps a user.")
                .Parameter("user")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        for (int i = 0; i < Config.INSTANCE.sleepedUsers.Count; i++) {
                            var user = Config.INSTANCE.sleepedUsers[i];
                            if (user.user_id == e.Message.MentionedUsers.First().Id) {
                                Config.INSTANCE.sleepedUsers.RemoveAt(i);
                                Config.INSTANCE.Commit();
                                e.Channel.SendMessage(e.Message.MentionedUsers.First().Mention + " is owoken");
                                e.Message.MentionedUsers.First().Edit(false, false);
                                return;
                            }
                        }
                        e.Channel.SendMessage(e.Message.MentionedUsers.First().Mention + " is not asleep");
                    }
                }
            );
        }

        public override void ParseMessageAsync(Channel channel, Message message) {
            if(isUserSleeped(message.User)) {
                message.Delete();
            }
        }
    }
}
