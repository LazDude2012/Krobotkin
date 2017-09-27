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
                egp.CreateCommand("list")
                .Do(async e => {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) >= 0) {
                        await e.Channel.SendMessage($"Please wait, compiling echo list. :clock2:");

                        // Compile echolist.html file
                        StreamWriter sw = new StreamWriter("echolist.html", false);
                        sw.Write(@"<!DOCTYPE html>
                        <html>
                        <head>
                            <title>Echo List</title>
                            <meta charset='utf-8'>
                            <style>
                            html, body{
                                margin: 0; padding: 0;
                            }

                            body{
                                background-color: #36393E;
                                color: #eee;
                                font-family: Whitney, Helvetica Neue, Helvetica, Arial, sans-serif;
                            }

                            h1{
                                text-align: center;
                            }

                            a{
                                text-decoration: none;
                                color: #46B7E4;
                            }

                            a:hover{
                                text-decoration: underline;
                            }

                            .echos{
                                margin: 40px auto;
                                max-width: 1400px;
                            }

                            .echo{
                                border-top: 1px solid #555;
                                padding-top: 25px;
                                margin-top: 25px;
                            }

                            .title{
                                font-weight: bold;
                                margin-bottom: 5px;
                            }

                            .content{
                                white-space: pre-wrap;
                                color: #aaa;
                            }
                            </style>
                        </head>
                        <body>
                            <h1>Echo List</h1>
                            <div class='echos'>
                        ");
                        foreach (EchoCommand ec in Config.INSTANCE.echoCommands) {
                            if (ec.server_id == e.Server.Id) {
                                if (ec.response.StartsWith("http://") || ec.response.StartsWith("https://")) {
                                    sw.WriteLine($"<div class='echo'><div class='title'>{ec.challenge}</div><div class='content'><a href='{ec.response}' target='_blank'>link</a></div></div>");
                                } else {
                                    sw.WriteLine($"<div class='echo'><div class='title'>{ec.challenge}</div><div class='content'>{ec.response}</div></div>");
                                }
                            }
                        }
                        sw.Write(@"
                            </div>
                            <script>
                            </script>
                        </body>
                        </html>");
                        sw.Close();

                        await e.Channel.SendMessage("List compiled! :robot: :tools:");
                        await e.Channel.SendFile("echolist.html");
                    }
                });
            });
        }
    }
}
