using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using System.Timers;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.IO;
using System.Drawing;
using ImageProcessor.Imaging;
using System.Text.RegularExpressions;
using Krobotkin.modules;
using Krobotkin;

namespace Krobotkin
{
    class Krobotkin
    {
        
        static string VERSION = "3.0";

        static void Main(string[] args) => new Krobotkin().Start();

        public static DiscordClient DiscordClient;

        private Timer HourlyTimer = new Timer();

        private List<ulong> UsersToKickFromBunker = new List<ulong>();

        /********************   IMPORTANT CHANNEL IDs ****************************/
        private ulong primary_server_id = 193389057210843137;

        public void Start()
        {
            DiscordClient = new DiscordClient();

            if(File.Exists("config.xml")) {
                using (FileStream fs = new FileStream("config.xml", FileMode.OpenOrCreate)) {
                    XmlSerializer reader = new XmlSerializer(typeof(Config));
                    Config.INSTANCE = (Config)reader.Deserialize(fs);
                }
            } else {
                Config.INSTANCE = new Config();
                Console.WriteLine("Did not find config, generating empty one");
            }

            HourlyTimer.Interval = 3600000;
            HourlyTimer.Elapsed += HourlyTimer_Elapsed;
            HourlyTimer.AutoReset = true;
            HourlyTimer.Start();
            DiscordClient.UserJoined += _client_UserJoined;
            DiscordClient.ServerAvailable += async (s, e) => {
                Console.WriteLine($"Joined Server {e.Server.Name}: {e.Server.Id}");
                try {
                    Channel general = (from channel in Config.INSTANCE.primaryChannels where channel.server_id == e.Server.Id select DiscordClient.GetChannel(channel.channel_id)).First();
                    await general.SendMessage($"Krobotkin {VERSION} initialised.");
                } catch (Exception) {
                    Console.WriteLine($"Failed to send greeting to {e.Server.Name}");
                }
            };

            var moduleTypes = from type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                              where typeof(Module).IsAssignableFrom(type) && type != typeof(Module)
                              select type;

            var modules = (from moduleType in moduleTypes select Activator.CreateInstance(moduleType)).ToList();

            DiscordClient.MessageReceived += (s, e) => {
                foreach (Module module in modules) {
                    module.ParseMessage(e.Channel, e.Message);
                }
            };

            DiscordClient.UsingCommands(x =>
           {
               x.PrefixChar = '!';
               x.HelpMode = HelpMode.Public;
           });

            foreach (Module module in modules) {
                module.InitiateClient(DiscordClient);
            }

            DiscordClient.GetService<CommandService>().CreateCommand("approve")
                .Parameter("user")
                .Description("FC Bunker Entrance only; approves user for FULLCOMM entry. Gives them the Gif and the invite, kicks them next hourly cycle.")
                .Do(async e =>
                {
                    if(e.Server.Id == 248993874666586142) // bunker entrance
                    {
                        Server fullcomm = DiscordClient.GetServer(primary_server_id);
                        await e.Channel.SendIsTyping();
                        await e.Channel.SendFile(@"C:\Users\lazdu\OneDrive\FULLCOMM SHARED\commie memes\approved.gif");
                        Invite inv = await fullcomm.CreateInvite(maxUses:1);
                        await e.Message.MentionedUsers.First().SendMessage(inv.Url);
                        await e.User.SendMessage(inv.Url);
                        UsersToKickFromBunker.Add(e.Message.MentionedUsers.First().Id);
                    }
                });

            DiscordClient.GetService<CommandService>().CreateCommand("forcenick")
                            .Alias("fn")
                            .Description("Forces a user's nickname to be changed.")
                            .Parameter("user")
                            .Parameter("nick")
                            .Do(async e =>
                            {
                                if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1)
                                {
                                    User user = e.Server.FindUsers(e.GetArg("user")).First();
                                    await user.Edit(nickname: e.GetArg("nick")); 
                                }
                            });

            DiscordClient.GetService<CommandService>().CreateCommand("quit")
                .Hide()
                .Do(e =>
               {
                   if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 2)
                   {
                       DiscordClient.Disconnect();
                       throw new Exception();
                   }
               });

            DiscordClient.GetService<CommandService>().CreateCommand("testwelcome")
                .Hide()
                .Parameter("user")
                .Do(e =>
                {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 2)
                    {
                        DisplayWelcomeMessage(e.Message.MentionedUsers.First());
                    }
                });

            foreach(EchoCommand ec in Config.INSTANCE.echoCommands)
            {
                DiscordClient.GetService<CommandService>().CreateCommand(ec.challenge)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(ec.response);
                    });
            }

            DiscordClient.GetService<CommandService>().CreateGroup("permissions", pgp =>
             {
                 pgp.CreateCommand("check")
                 .Description("Informs a user of their permission level.")
                 .Do(e =>
                 {
                     int perms = Config.INSTANCE.GetPermissionLevel(e.User, e.Server);
                     switch (perms)
                     {
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
                 });
                 pgp.CreateCommand("set")
                 .Parameter("role")
                 .Parameter("level")
                 .Description("Sets a user's permission level.")
                 .Do(e =>
                 {
                     if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > Int32.Parse(e.GetArg("level")))
                     {
                         Role r = e.Server.FindRoles(e.GetArg("role")).First();
                         foreach (ConfigRole cr in Config.INSTANCE.roles)
                         {
                             if (cr.role_id == r.Id) cr.trust_level = Int32.Parse(e.GetArg("level"));
                         }
                     }
                     Config.INSTANCE.Commit();
                 });
             });

            DiscordClient.GetService<CommandService>().CreateCommand("mountaintime")
                .Alias("time", "laztime")
                .Description("Gives the time on Laz's PC.")
                .Do(async e =>
               {
                   await e.Channel.SendMessage($"The current time is {System.DateTime.Now.ToLongTimeString()} MDT.");
               });

            DiscordClient.GetService<CommandService>().CreateCommand("kick")
                .Description("Kicks a user.")
                .Parameter("user")
                .Do(async e =>
                {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1)
                    {
                        String usersKicked = "";
                        foreach (User user in e.Message.MentionedUsers)
                        {
                            if (!user.IsBot)
                            {
                                usersKicked += (user.Name + " ");
                                await user.Kick();
                            }
                        }
                        ModerationLog.LogToCabal($"User {e.User.Name} kicked user(s) {usersKicked}", e.Server); 
                    }
                });

            DiscordClient.GetService<CommandService>().CreateCommand("ban")
                .Description("Bans a user.")
                .Parameter("user")
                .Do(async e =>
                {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1)
                    {
                        String usersBanned = "";
                        foreach (User user in e.Message.MentionedUsers)
                        {
                            if (!user.IsBot)
                            {
                                usersBanned += (user.Name + " ");
                                await e.Server.Ban(user, 3);
                            }
                        }
                        ModerationLog.LogToCabal($"User {e.User.Name} banned user(s) {usersBanned}", e.Server);
                    }
                });

            DiscordClient.GetService<CommandService>().CreateCommand("purge")
                .Description("Clears messages from a channel.")
                .Parameter("number", type: ParameterType.Required)
                .Parameter("user", ParameterType.Optional)
                .Do(async e =>
                {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1)
                    {
                        var purgemessages = await e.Channel.DownloadMessages(Int32.Parse(e.Args[0]) + 1);
                        if(e.GetArg("user") == "")
                        await e.Channel.DeleteMessages(purgemessages);
                        else
                        {
                            foreach(Message msg in purgemessages)
                            {
                                if (msg.User == e.Message.MentionedUsers.First()) await msg.Delete();
                            }
                        } 
                    }
                    await e.Message.Delete();
                    ModerationLog.LogToCabal($"User {e.User.Name} purged {e.GetArg("number")} messages in #{e.Channel.Name}", e.Server);
                });

            DiscordClient.GetService<CommandService>().CreateCommand("delet")
                .Description("Clears messages from a channel.")
                .Parameter("number")
                .Parameter("user", ParameterType.Optional)
                .Do(async e =>
                {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1)
                    {
                        var purgemessages = await e.Channel.DownloadMessages(Int32.Parse(e.Args[0]) + 1);
                        if (e.GetArg("user") == "")
                            await e.Channel.DeleteMessages(purgemessages);
                        else
                        {
                            foreach (Message msg in purgemessages)
                            {
                                if (msg.User == e.Message.MentionedUsers.First()) await msg.Delete();
                            }
                        }
                        await e.Message.Delete();
                    }

                    ModerationLog.LogToCabal($"User {e.User.Name} delet'd {e.GetArg("number")} messages in #{e.Channel.Name}", e.Server);
                });

            DiscordClient.GetService<CommandService>().CreateGroup("blacklist", bgp =>
            {
                bgp.CreateCommand("print")
                .Do(async e =>
                {
                    await e.Channel.SendIsTyping();
                    await e.Channel.SendMessage("+++++++++++++++++ CURRENT BLACKLIST +++++++++++++++++");
                    foreach(String word in Config.INSTANCE.Blacklist)
                    {
                        await e.Channel.SendMessage(word);
                    }
                    await e.Channel.SendMessage("++++++++++++++++++ BLACKLIST ENDS +++++++++++++++++++");
                });
                bgp.CreateCommand("add")
                .Parameter("word")
                .Do(e =>
                {
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) > 1)
                    {
                        Config.INSTANCE.Blacklist.Add(e.GetArg("word"));
                        ModerationLog.LogToCabal($"User {e.User} added the word {e.Args[0]} to the blacklist.", e.Server);
                        Config.INSTANCE.Commit();
                    }
                });
                bgp.CreateCommand("remove")
                .Parameter("word")
                .Do(e =>
                {
                    if(Config.INSTANCE.GetPermissionLevel(e.User,e.Server) > 1)
                    {
                        Config.INSTANCE.Blacklist.Remove(e.Args[0]);
                        ModerationLog.LogToCabal($"User {e.User} removed the word {e.Args[0]} from the blacklist.", e.Server);
                    }
                });
            });

            /*
            _client.GetService<CommandService>().CreateCommand("meme")
                .Description("Uploads a meme from Laz's meme folder.")
                .Parameter("filename")
                .Do(async e =>
                {
                    if (GetPermissionLevel(e.User,e.Server) > 1)
                    {
                        await e.Channel.SendIsTyping();
                        try
                        {
                            await e.Channel.SendFile(@"C:\Users\lazdu\OneDrive\FULLCOMM Shared\commie memes\" + e.GetArg("filename") + (e.GetArg("filename").EndsWith(".png") ? "" : ".jpg"));
                        }
                        catch (Exception)
                        {
                            await e.Channel.SendMessage("Sorry, pal, no can do.");
                        }
                    }
                });

            _client.GetService<CommandService>().CreateCommand("gif")
                .Description("Uploads a GIF from Laz's meme folder.")
                .Parameter("filename")
                .Do(async e =>
                {
                    if (GetPermissionLevel(e.User,e.Server) > 1)
                    {
                        await e.Channel.SendIsTyping();
                        try
                        {
                            await e.Channel.SendFile(@"C:\Users\lazdu\OneDrive\FULLCOMM Shared\commie memes\" + e.GetArg("filename") + ".gif");
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Sorry, pal, no can do.");
                        }
                    }
                }
            );
            */

            DiscordClient.Connect(Config.INSTANCE.bot_token, TokenType.Bot);
            Console.WriteLine("CONNECTED");
            Console.WriteLine("Commands:");
            Console.WriteLine("info <serverid> - view info about server");
            while(true) {
                var input = Console.ReadLine();
                if(input.StartsWith("info ")) {
                    var inputParams = input.Split(' ');
                    var serverid = ulong.Parse(inputParams[1]);

                    var server = (from serv in DiscordClient.Servers where serv.Id == serverid select serv).First();

                    Console.WriteLine($"Name: {server.Name}");
                    Console.WriteLine("Roles:");
                    foreach(Role role in server.Roles) {
                        Console.WriteLine($"  Role Name: {role.Name}");
                        Console.WriteLine($"  Role Id: {role.Id}");
                        Console.WriteLine($"  Role Colour: {role.Color}");
                        Console.WriteLine();
                    }
                    Console.WriteLine("Text Channels:");
                    foreach (Channel channel in server.TextChannels) {
                        Console.WriteLine($"\t{channel.Name}: {channel.Id}");
                    }
                    Console.WriteLine();
                    Console.WriteLine("Voice Channels:");
                    foreach (Channel channel in server.VoiceChannels) {
                        Console.WriteLine($"\t{channel.Name}: {channel.Id}");
                    }
                }
            }
        }

        private void _client_UserJoined(object sender, UserEventArgs e)
        {
            if (e.User.Name == "totallydialectical" && e.User.Discriminator == 8958)
            {
                e.Server.Ban(e.User); //bans d3crypt
                ModerationLog.LogToCabal("d3crypt ban script triggered; d3crypt banned", DiscordClient.GetServer(primary_server_id));
            }
            if (e.Server.Id == primary_server_id) DisplayWelcomeMessage(e.User);
        }

        private void DisplayWelcomeMessage(User user)
        {
            byte[] avatar = null;
            using( var wc = new System.Net.WebClient())
            {
                avatar = (user.AvatarUrl == null) ? null : wc.DownloadData(user.AvatarUrl);
                if (avatar == null)
                {
                    DiscordClient.GetChannel(
                        (from channel in Config.INSTANCE.primaryChannels where channel.server_id == user.Server.Id select channel.channel_id).First()
                    ).SendMessage("Welcome new comrade" + user.Mention);
 
                    return;
                }
            }
            var astream = new MemoryStream(avatar);
            Image ai = Image.FromStream(astream);
            var outstream = new MemoryStream();
            using(var ifact = new ImageProcessor.ImageFactory())
            {
                //159,204 image size 283x283
                ImageLayer ilay = new ImageLayer() {
                    Image = ai,
                    Size = new Size(283, 283),
                    Position = new Point(159, 204)
                };
                ifact.Load("welcome.jpg");
                ifact.Overlay(ilay);
                System.Drawing.Color yellow = System.Drawing.Color.FromArgb(208,190,25);
                TextLayer uname = new TextLayer() { Position = new Point(108, 512), FontFamily = FontFamily.GenericSansSerif, FontSize = 30, Text = user.Nickname, FontColor = yellow };
                ifact.Watermark(uname);
                ifact.Save(outstream);
            }
            Channel general = DiscordClient.GetChannel((from channel in Config.INSTANCE.primaryChannels where channel.server_id == user.Server.Id select channel.channel_id).First());
            general.SendMessage("Welcome new comrade " + user.Mention);
            general.SendFile("welcome.jpg", outstream);
            ModerationLog.LogToCabal($"User {user} joined.", DiscordClient.GetServer(user.Server.Id));
        }

        private async void HourlyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach(Channel photoDeleteChannel in (from channel in Config.INSTANCE.deletePhotoChannels select DiscordClient.GetChannel(channel.channel_id))) {
                if(photoDeleteChannel != null) {
                    Message[] buffer = await photoDeleteChannel.DownloadMessages(100);
                    int messagesRemoved = 0;
                    foreach (Message m in buffer) {
                        if (m.Attachments.Length != 0) {
                            await m.Delete();
                            messagesRemoved++;
                        }
                    }
                    if (messagesRemoved != 0) ModerationLog.LogToCabal($"Hourly purge of selfies removed {messagesRemoved} messages.", photoDeleteChannel.Server);
                }

                foreach (Channel channel in from reminderChannel in Config.INSTANCE.hourlyReminderChannels
                                            select DiscordClient.GetChannel(reminderChannel.channel_id)
                ) {
                    if(channel != null) {
                        await channel.SendMessage(Config.INSTANCE.hourlyReminders[new Random().Next() % Config.INSTANCE.hourlyReminders.Count]);
                    }
                }
            }
        }
    }
}
