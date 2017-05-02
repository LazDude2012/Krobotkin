using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.IO;

namespace KrobotkinDiscord.Modules {
    public class Echo : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateGroup("echo", egp => {
                egp.CreateCommand("add")
                .Parameter("challenge")
                .Parameter("response")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 0) {
                        EchoCommand ec = new EchoCommand { challenge = e.Args[0], response = e.Args[1] };
                        Config.INSTANCE.echoCommands.Add(ec);
                        _client.GetService<CommandService>().CreateCommand(ec.challenge)
                        .Do(async f => {
                            await f.Channel.SendMessage(ec.response);
                        });
                        Config.INSTANCE.Commit();
                    } else {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                    }
                });
                egp.CreateCommand("list")
                .Do(async e => {
                    int progress = 1;
                    Message compiling = await e.Channel.SendMessage($"Please wait, compiling echo list. :clock{progress}:");
                    StreamWriter sw = new StreamWriter("echolist.html", false);
                    sw.Write("<html>\n<body>\n<h1>ECHO LIST</h1>\n");
                    foreach (EchoCommand ec in Config.INSTANCE.echoCommands) {
                        if (ec.response.StartsWith("http")) {
                            sw.WriteLine($"<p>{ec.challenge} :&gt; <a href='{ec.response}'> link </a> </p><br/>");
                        } else sw.WriteLine($"<p>{ec.challenge} :&gt; {ec.response}</p><br/>");
                        progress = (progress == 12 ? 1 : progress + 1);
                        await compiling.Edit($"Please wait, compiling echo list. :clock{progress}:");
                    }
                    sw.Write("</body>");
                    sw.Close();
                    await e.Channel.SendMessage("List compiled! :robot: :tools:");
                    await e.Channel.SendFile("echolist.html");
                });
                egp.CreateCommand("remove")
                .Parameter("challenge")
                .Do(e => {
                    foreach (EchoCommand c in Config.INSTANCE.echoCommands) {
                        if (c.challenge == e.Args[0]) {
                            Config.INSTANCE.echoCommands.Remove(c);
                            break;
                        }
                    }
                    e.Channel.SendMessage($"The echo {e.Args[0]} will be removed on the next restart of Krobotkin.");
                    Config.INSTANCE.Commit();
                });
            });
        }
    }
}
