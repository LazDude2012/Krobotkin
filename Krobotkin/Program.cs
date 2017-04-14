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
            
namespace LazDude2012.Krobotkin
{
    class Krobotkin
    {
        
        static string version = "2.1";
        static bool startedup = false;
        static void Main(string[] args) => new Krobotkin().Start();

        private DiscordClient _client;

        private Timer hourlyTimer = new Timer();

        private List<ulong> usersToKickFromBunker = new List<ulong>();
        
        private string convertToFullwidth(string text)
        {
            string output = "";

            for (var i = 0; i < text.Length; i++)
            {
                char normal = text[i];
                try
                {
                    char latin = config.letters[normal];
                    output += latin;
                }
                catch (KeyNotFoundException e)
                {
                    output += normal;
                }
            }

            return output;
        }
		private string findText(string text, string author)
		{
			string output = "";

			if (Directory.Exists("corpus") && Directory.Exists("corpus/" + author))
			{
				string[] files = Directory.GetFiles("corpus/" + author);
                int resultsTruncated = 0;
                foreach (string file in files)
				{
					string fullFileText = File.ReadAllText(file);
                    char[] fullStop = { '.', '!', '?', ';' };
                    string[] fileLines = fullFileText.Split(fullStop);
					foreach (string line in fileLines)
					{
						if (Regex.IsMatch(line, text, RegexOptions.IgnoreCase))
                        {							
                            if (output.Length > 900)
                            {
                                ++resultsTruncated;
                            }
                            else output += file.Substring(file.LastIndexOfAny(new char[] { '\\', '/' }) + 1, 
                                (file.LastIndexOf(".", StringComparison.Ordinal) - file.LastIndexOfAny(new char[] { '\\', '/' }) -1)) + ": " + line.Trim(' ', '\r', '\n', '\t') + "\n";
                        }
					}
                    

				}
                if (resultsTruncated != 0) output += $"{resultsTruncated} more results were found but omitted for space.";
			}
			else
			{
					output = "Sorry, Krobotkin has no text for " + author;
			}
			return output;
		}

        private Config config = new Config();

        /********************   IMPORTANT CHANNEL IDs ****************************/
        private ulong primary_server_id = 193389057210843137;
        private ulong mainchannel_id = 276394952660090881;
        private ulong moderation_log_id = 243537594393034754;
        private ulong photo_delete_channel_id = 276395301881905162;


        public void Start()
        {
            _client = new DiscordClient();

            using (FileStream fs = new FileStream("config.xml",FileMode.OpenOrCreate))
            {
                XmlSerializer reader = new XmlSerializer(typeof(Config));
                config = (Config)reader.Deserialize(fs); 
            }
           
            hourlyTimer.Interval = 3600000;
            hourlyTimer.Elapsed += MorrowindTimer_Elapsed;
            hourlyTimer.AutoReset = true;
            hourlyTimer.Start();
            _client.UserJoined += _client_UserJoined;
            _client.MessageReceived += async (s, e) =>
            {
                if(!startedup)
                {
                    Channel general = _client.GetChannel(mainchannel_id);
                    await general.SendMessage($"Krobotkin {version} initialised.");
                    startedup = true;
                }
                foreach(String word in config.Blacklist)
                {
                    if (e.Message.Text.ToLower().Contains(word) && !e.User.IsBot && GetPermissionLevel(e.User, e.Server) < 2)
                    {
                        LogToCabal($"User {e.User.Name} used blacklisted word {word} in channel #{e.Channel.Name}. Message was as follows: \n {e.Message}", e.Server);
                        await e.Channel.DeleteMessages(new Message[] { e.Message });
                        await e.Channel.SendMessage("``` Message redacted for the sake of the Motherland. ```");
                        break;
                    }
                }
                if(e.Message.Text.StartsWith("!aes"))
                {
                    await e.Channel.SendIsTyping();
                    String msg = (e.Message.Text.Length >= 6) ? e.Message.Text.Substring(5) : "empty like my soul";
                    String fwidth = convertToFullwidth(msg);
                    await e.Channel.SendMessage(fwidth);
                }
				if (e.Message.Text.StartsWith("!kropotkin"))
				{
					await e.Channel.SendIsTyping();
					if (e.Message.Text.Length >= 12)
					{
						String phrase = e.Message.Text.Substring(11);
						String results = findText(phrase, "kropotkin");
                        if (results == "") await e.Channel.SendMessage("None found.");
						else await e.Channel.SendMessage(results);
					}
					else
					{
						await e.Channel.SendMessage("Command requires a search parameter.");
					}

				}
                if(e.Message.Text.StartsWith("!mball "))
                {
                    await e.Channel.SendIsTyping();
                    using (MemoryStream memstream = new MemoryStream())
                    {

                        try
                        {
                            await ProcessMemeballMeme(e.Message.Text.Substring(7), memstream);
                            await e.Channel.SendFile("meme.png", memstream);
                        }
                        catch (Exception)
                        {
                            await e.Channel.SendMessage("Sorry Dave, I can't do that. :/");
                        }
                    }
                }
                if(e.Message.Text.ToLower() == "ayy")
                {
                    if (GetPermissionLevel(e.User,e.Server) > 0)
                    {
                        await e.Channel.SendIsTyping();
                        await e.Channel.SendFile("lmao.gif"); 
                    }
                }
                if (e.Message.Text.ToLower() == "debug init")
                    if (GetPermissionLevel(e.User, e.Server) > 2)
                    {
                        await e.Channel.SendMessage("Debug information printed; config file initialized. All role permissions reset.");
                        foreach(Server serv in _client.Servers)
                        {
                            foreach(Role r in serv.Roles)
                            {
                                config.roles.Add(new ConfigRole { name = $"{serv.Name} -> {r.Name}", role_id = r.Id, server_id = serv.Id, trust_level = 0 });
                            }
                        }
                    }
            };

            #region Commands
            _client.UsingCommands(x =>
           {
               x.PrefixChar = '!';
               x.HelpMode = HelpMode.Public;
           });

            _client.GetService<CommandService>().CreateCommand("approve")
                .Parameter("user")
                .Description("FC Bunker Entrance only; approves user for FULLCOMM entry. Gives them the Gif and the invite, kicks them next hourly cycle.")
                .Do(async e =>
                {
                    if(e.Server.Id == 248993874666586142) // bunker entrance
                    {
                        Server fullcomm = _client.GetServer(primary_server_id);
                        await e.Channel.SendIsTyping();
                        await e.Channel.SendFile(@"C:\Users\lazdu\OneDrive\FULLCOMM SHARED\commie memes\approved.gif");
                        Invite inv = await fullcomm.CreateInvite(maxUses:1);
                        await e.Message.MentionedUsers.First().SendMessage(inv.Url);
                        await e.User.SendMessage(inv.Url);
                        usersToKickFromBunker.Add(e.Message.MentionedUsers.First().Id);
                    }
                });

            _client.GetService<CommandService>().CreateCommand("forcenick")
                            .Alias("fn")
                            .Description("Forces a user's nickname to be changed.")
                            .Parameter("user")
                            .Parameter("nick")
                            .Do(async e =>
                            {
                                if (GetPermissionLevel(e.User, e.Server) > 1)
                                {
                                    User user = e.Server.FindUsers(e.GetArg("user")).First();
                                    await user.Edit(nickname: e.GetArg("nick")); 
                                }
                            });

            _client.GetService<CommandService>().CreateCommand("quit")
                .Hide()
                .Do(e =>
               {
                   if (GetPermissionLevel(e.User, e.Server) > 2)
                   {
                       _client.Disconnect();
                       throw new Exception();
                   }
               });

            _client.GetService<CommandService>().CreateCommand("testwelcome")
                .Hide()
                .Parameter("user")
                .Do(e =>
                {
                    if (GetPermissionLevel(e.User,e.Server)>2)
                    {
                        DisplayWelcomeMessage(e.Message.MentionedUsers.First());
                    }
                });

            foreach(EchoCommand ec in config.echoCommands)
            {
                _client.GetService<CommandService>().CreateCommand(ec.challenge)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(ec.response);
                    });
            }

            _client.GetService<CommandService>().CreateGroup("permissions", pgp =>
             {
                 pgp.CreateCommand("check")
                 .Description("Informs a user of their permission level.")
                 .Do(e =>
                 {
                     int perms = GetPermissionLevel(e.User, e.Server);
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
                     if (GetPermissionLevel(e.User, e.Server) > Int32.Parse(e.GetArg("level")))
                     {
                         Role r = e.Server.FindRoles(e.GetArg("role")).First();
                         foreach (ConfigRole cr in config.roles)
                         {
                             if (cr.role_id == r.Id) cr.trust_level = Int32.Parse(e.GetArg("level"));
                         }
                     }
                     Reserialise();
                 });
             });

            _client.GetService<CommandService>().CreateGroup("echo", egp =>
            {
                egp.CreateCommand("add")
                .Parameter("challenge")
                .Parameter("response")
                .Do(e =>
                {
                    if (GetPermissionLevel(e.User, e.Server) > 0)
                    {
                        EchoCommand ec = new EchoCommand { challenge = e.Args[0], response = e.Args[1] };
                        config.echoCommands.Add(ec);
                        _client.GetService<CommandService>().CreateCommand(ec.challenge)
                        .Do(async f =>
                        {
                            await f.Channel.SendMessage(ec.response);
                        });
                        Reserialise(); 
                    }
                    else
                    {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                    }
                });
                egp.CreateCommand("list")
                .Do(async e =>
                {
                    int progress = 1;
                    Message compiling = await e.Channel.SendMessage($"Please wait, compiling echo list. :clock{progress}:");
                    StreamWriter sw = new StreamWriter("echolist.html", false);
                    sw.Write("<html>\n<body>\n<h1>ECHO LIST</h1>\n");
                    foreach(EchoCommand ec in config.echoCommands)
                    {
                        if (ec.response.StartsWith("http"))
                        {
                            sw.WriteLine($"<p>{ec.challenge} :&gt; <a href='{ec.response}'> link </a> </p><br/>");
                        }
                        else sw.WriteLine($"<p>{ec.challenge} :&gt; {ec.response}</p><br/>");
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
                .Do(e =>
                {
                    foreach (EchoCommand c in config.echoCommands)
                    {
                        if (c.challenge == e.Args[0])
                        {
                            config.echoCommands.Remove(c);
                            break;
                        }
                    }
                    e.Channel.SendMessage($"The echo {e.Args[0]} will be removed on the next restart of Krobotkin.");
                    Reserialise();
                });
            });

            _client.GetService<CommandService>().CreateCommand("mountaintime")
                .Alias("time", "laztime")
                .Description("Gives the time on Laz's PC.")
                .Do(async e =>
               {
                   await e.Channel.SendMessage($"The current time is {System.DateTime.Now.ToLongTimeString()} MDT.");
               });

            _client.GetService<CommandService>().CreateCommand("kick")
                .Description("Kicks a user.")
                .Parameter("user")
                .Do(async e =>
                {
                    if (GetPermissionLevel(e.User, e.Server) > 1)
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
                        LogToCabal($"User {e.User.Name} kicked user(s) {usersKicked}", e.Server); 
                    }
                });

            _client.GetService<CommandService>().CreateCommand("ban")
                .Description("Bans a user.")
                .Parameter("user")
                .Do(async e =>
                {
                    if (GetPermissionLevel(e.User, e.Server) > 1)
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
                        LogToCabal($"User {e.User.Name} banned user(s) {usersBanned}", e.Server);
                    }
                });

            _client.GetService<CommandService>().CreateCommand("purge")
                .Description("Clears messages from a channel.")
                .Parameter("number", type: ParameterType.Required)
                .Parameter("user", ParameterType.Optional)
                .Do(async e =>
                {
                    if (GetPermissionLevel(e.User, e.Server) > 1)
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
                    LogToCabal($"User {e.User.Name} purged {e.GetArg("number")} messages in #{e.Channel.Name}", e.Server);
                });

            _client.GetService<CommandService>().CreateCommand("delet")
                .Description("Clears messages from a channel.")
                .Parameter("number")
                .Parameter("user", ParameterType.Optional)
                .Do(async e =>
                {
                    if (GetPermissionLevel(e.User, e.Server) > 1)
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
                    
                    LogToCabal($"User {e.User.Name} delet'd {e.GetArg("number")} messages in #{e.Channel.Name}", e.Server);
                });

            _client.GetService<CommandService>().CreateGroup("blacklist", bgp =>
            {
                bgp.CreateCommand("print")
                .Do(async e =>
                {
                    await e.Channel.SendIsTyping();
                    await e.Channel.SendMessage("+++++++++++++++++ CURRENT BLACKLIST +++++++++++++++++");
                    foreach(String word in config.Blacklist)
                    {
                        await e.Channel.SendMessage(word);
                    }
                    await e.Channel.SendMessage("++++++++++++++++++ BLACKLIST ENDS +++++++++++++++++++");
                });
                bgp.CreateCommand("add")
                .Parameter("word")
                .Do(e =>
                {
                    if (GetPermissionLevel(e.User, e.Server) > 1)
                    {
                        config.Blacklist.Add(e.GetArg("word"));
                        LogToCabal($"User {e.User} added the word {e.Args[0]} to the blacklist.", e.Server);
                        Reserialise();
                    }
                });
                bgp.CreateCommand("remove")
                .Parameter("word")
                .Do(e =>
                {
                    if(GetPermissionLevel(e.User,e.Server) > 1)
                    {
                        config.Blacklist.Remove(e.Args[0]);
                        LogToCabal($"User {e.User} removed the word {e.Args[0]} from the blacklist.", e.Server);
                    }
                });
            });

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
                });

            #endregion

            _client.ExecuteAndWait(async () => {
                await _client.Connect(config.bot_token, TokenType.Bot);
                Console.WriteLine("CONNECTED");
            });
        }

        private int GetPermissionLevel(User user, Server server)
        {
            if (user.Id == 159017676662898696) return 3; //laz always gets highest powers
            else
            {
                int ret = 0;
                foreach (ConfigRole r in config.roles)
                {
                    if (server.GetRole(r.role_id) == null) continue;
                    else
                    {
                        if (user.HasRole(server.GetRole(r.role_id)) && r.trust_level > ret)
                            ret = r.trust_level;
                    }
                }
                return ret;
            }
        }

        private void Reserialise()
        {
            using (FileStream fs = new FileStream("config.xml", FileMode.Create))
            {
                XmlSerializer writer = new XmlSerializer(typeof(Config));
                writer.Serialize(fs, config);
            }
        }

        private Task ProcessMemeballMeme(string text, MemoryStream stream)
        {

            using (ImageProcessor.ImageFactory ifact = new ImageProcessor.ImageFactory())
            {
                //TEXT WILL BE 32 CHARACTERS PER LINE, AT A 30 PIXEL HEIGHT IN ARIAL FONT
                //IMAGE ITSELF IS 450 PIXELS HIGH, ADD 34 PX PER LINE OF TEXT
                //TOTAL CANVAS IS 850 PX HIGH,
                String[] words = text.Split(' ');
                ifact.Load(words[0] + ".png");
                string memetext = "";
                int lines = 0;
                string currentline = "";
                for (int i = 1; i < words.Length; ++i)
                {
                    string word = words[i];
                    if((currentline + word).Length >= 32)
                    {
                        memetext += (currentline + "\n");
                        ++lines;
                        currentline = "";
                    }
                    currentline += (word + " ");
                }
                memetext += currentline;
                TextLayer tl = new TextLayer();
                tl.Position = new Point(68, (380 - (34 * lines)));
                tl.FontSize = 30;
                tl.FontFamily = FontFamily.GenericSansSerif;
                tl.Text = memetext;
                tl.FontColor = System.Drawing.Color.Black;
                ifact.Watermark(tl);
                ifact.Crop(new Rectangle(0, (374 - (34 * lines)), 592, 850 - (374 - (34 * lines))));
                ifact.Save(stream);
                return Task.FromResult<object>(null);
            }
        }

        private void _client_UserJoined(object sender, UserEventArgs e)
        {
            if (e.User.Name == "totallydialectical" && e.User.Discriminator == 8958)
            {
                e.Server.Ban(e.User); //bans d3crypt
                LogToCabal("d3crypt ban script triggered; d3crypt banned", _client.GetServer(primary_server_id));
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
                    _client.GetChannel(mainchannel_id).SendMessage("Welcome new comrade" + user.Mention);
 
                    return;
                }
            }
            var astream = new MemoryStream(avatar);
            Image ai = Image.FromStream(astream);
            var outstream = new MemoryStream();
            using(var ifact = new ImageProcessor.ImageFactory())
            {
                //159,204 image size 283x283
                ImageLayer ilay = new ImageLayer();
                ilay.Image = ai;
                ilay.Size = new Size(283, 283);
                ilay.Position = new Point(159, 204);
                ifact.Load("welcome.jpg");
                ifact.Overlay(ilay);
                System.Drawing.Color yellow = System.Drawing.Color.FromArgb(208,190,25);
                TextLayer uname = new TextLayer() { Position = new Point(108, 512), FontFamily = FontFamily.GenericSansSerif, FontSize = 30, Text = user.Nickname, FontColor = yellow };
                ifact.Watermark(uname);
                ifact.Save(outstream);
            }
            Channel general = _client.GetChannel(mainchannel_id);
            general.SendMessage("Welcome new comrade " + user.Mention);
            general.SendFile("welcome.jpg", outstream);
            LogToCabal($"User {user} joined.", _client.GetServer(primary_server_id));
        }

        private async void MorrowindTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Channel selfiedarity = _client.GetChannel(photo_delete_channel_id);
            Message[] buffer = await selfiedarity.DownloadMessages(100);
            int messagesRemoved = 0;
            foreach(Message m in buffer)
            {
                if (m.Attachments.Length != 0)
                {
                    await m.Delete();
                    messagesRemoved++;
                }
            }
            if(messagesRemoved != 0) LogToCabal($"Hourly purge of selfies removed {messagesRemoved} messages.", _client.GetServer(primary_server_id));
            Channel general = _client.GetChannel(mainchannel_id);
            await general.SendMessage(config.hourlyReminders[new Random().Next() % config.hourlyReminders.Count]);
        }

        private async void LogToCabal(String logMessage,Server server)
        {
            Channel cabal = server.GetChannel(moderation_log_id); 
            await cabal.SendMessage($"``` {logMessage} ```");
        }
    }
}
