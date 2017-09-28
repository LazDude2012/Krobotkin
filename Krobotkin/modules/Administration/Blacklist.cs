using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace KrobotkinDiscord.Modules.Administration {
    class Blacklist : Module {
        
        public const int MESSAGE_MAX_LENGTH = 2000;

        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateGroup("blacklist", bgp => {
                bgp.CreateCommand("print")
                .Do(async e => {
                    await e.Channel.SendIsTyping();
                    
                    String line = "";
                    line += "================== CURRENT BLACKLIST ================\n";
                    line += $"   Blacklist contains {Config.INSTANCE.Blacklist.Count} words\n";
                    line += "==================================================\n";

                    foreach (String word in Config.INSTANCE.Blacklist) {
                        if (line.Length + word.Length + 1 <= MESSAGE_MAX_LENGTH){
                            line += word + '\n';
                        } else {
                            await e.Channel.SendMessage(line);
                            line = word + '\n';
                        }
                    }
                    if (line.Length > 0) await e.Channel.SendMessage(line);

                    await e.Channel.SendMessage("================== BLACKLIST ENDS ===================");
                });
                bgp.CreateCommand("add")
                .Parameter("word")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        //String word = e.GetArg("word");
                        //bool is_pattern = word.StartsWith("/") && word.EndsWith("/");
                        
                        if (Config.INSTANCE.Blacklist.Contains(e.GetArg("word"))){
                            e.Channel.SendMessage($"Word \"{e.Args[0]}\" already in blacklist.");
                            return;
                        }

                        Config.INSTANCE.Blacklist.Add(e.GetArg("word"));
                        ModerationLog.LogToPublic($"User {e.User} added the word {e.Args[0]} to the blacklist.", e.Server);
                        e.Channel.SendMessage($"Added \"{e.Args[0]}\" to the blacklist.");
                        Config.INSTANCE.Commit();
                    }
                });
                bgp.CreateCommand("remove")
                .Parameter("word")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1) {
                        if (!Config.INSTANCE.Blacklist.Contains(e.GetArg("word"))){
                            e.Channel.SendMessage($"Word \"{e.Args[0]}\" not in blacklist.");
                            return;
                        }

                        Config.INSTANCE.Blacklist.Remove(e.Args[0]);
                        ModerationLog.LogToPublic($"User {e.User} removed the word {e.Args[0]} from the blacklist.", e.Server);
                        e.Channel.SendMessage($"Removed \"{e.Args[0]}\" from the blacklist.");
                        Config.INSTANCE.Commit();
                    }
                });
            });
        }
    }
}
