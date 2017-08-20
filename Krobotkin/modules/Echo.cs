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
        public override async void ParseMessageAsync(Channel channel, Message message) {
            if (message.Text.StartsWith("!")) {
                foreach (var echo in Config.INSTANCE.echoCommands) {
                    if (echo.challenge == message.Text.Substring(1).Trim()) {
                        if (echo.server_id == 0) {
                            echo.server_id = Program.PRIMARY_SERVER_ID;
                            Config.INSTANCE.Commit();
                        }
                        if (echo.server_id == message.Server.Id) {
                            await message.Channel.SendMessage(echo.response);
                        }
                    }
                }
            }
        }
        public override void InitiateClient(DiscordClient _client) {

            _client.GetService<CommandService>().CreateGroup("echo", egp => {
                egp.CreateCommand("add")
                .Parameter("challenge")
                .Parameter("response")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 0) {
                        EchoCommand ec = new EchoCommand {
                            challenge = e.Args[0],
                            response = e.Args[1],
                            server_id = e.Server.Id
                        };
                        if(Config.INSTANCE.echoCommands.Contains(ec)) {
                            e.Channel.SendMessage("Cannot add echo !" + ec.challenge + "... already exists");
                            return;
                        }
                        Config.INSTANCE.echoCommands.Add(ec);
                        Config.INSTANCE.Commit();
                        e.Channel.SendMessage("Added echo !" + ec.challenge + " : " + ec.response);
                    } else {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                    }
                });
                egp.CreateCommand("list")
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) >= 0) {
                        int progress = 1;
                        Message compiling = await e.Channel.SendMessage($"Please wait, compiling echo list. :clock{progress}:");
                        StreamWriter sw = new StreamWriter("echolist.html", false);
                        sw.Write("<html>\n<body>\n<h1>ECHO LIST</h1>\n");
                        foreach (EchoCommand ec in Config.INSTANCE.echoCommands) {
                            if(ec.server_id == e.Server.Id) {
                                if (ec.response.StartsWith("http")) {
                                    sw.WriteLine($"<p>{ec.challenge} :&gt; <a href='{ec.response}'> link </a> </p><br/>");
                                } else sw.WriteLine($"<p>{ec.challenge} :&gt; {ec.response}</p><br/>");
                                progress = (progress == 12 ? 1 : progress + 1);
                                await compiling.Edit($"Please wait, compiling echo list. :clock{progress}:");
                            }
                        }
                        sw.Write("</body>");
                        sw.Close();
                        await e.Channel.SendMessage("List compiled! :robot: :tools:");
                        await e.Channel.SendFile("echolist.html");
                    }
                });
                egp.CreateCommand("remove")
                .Parameter("challenge")
                .Do(e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) >= 2) {
                        foreach (EchoCommand c in Config.INSTANCE.echoCommands) {
                            if (c.challenge == e.Args[0]) {
                                Config.INSTANCE.echoCommands.Remove(c);
                                break;
                            }
                        }
                        e.Channel.SendMessage($"The echo {e.Args[0]} has been removed.");
                        Config.INSTANCE.Commit();
                    }
                });
            });
        }
    }
}
