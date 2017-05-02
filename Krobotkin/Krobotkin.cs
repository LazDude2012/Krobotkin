using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Timers;
using Discord;
using Discord.Commands;
using System.IO;
using System.Drawing;
using ImageProcessor.Imaging;

namespace Krobotkin
{
    class Krobotkin
    {
        static string VERSION = "3.0";

        static void Main(string[] args) => new Krobotkin().Start();

        public static DiscordClient DiscordClient;

        private Timer HourlyTimer = new Timer();

        public static List<ulong> UsersToKickFromBunker = new List<ulong>();

        /********************   IMPORTANT CHANNEL IDs ****************************/
        public const ulong PRIMARY_SERVER_ID = 193389057210843137;

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

            foreach(EchoCommand ec in Config.INSTANCE.echoCommands)
            {
                DiscordClient.GetService<CommandService>().CreateCommand(ec.challenge)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(ec.response);
                    });
            }

            DiscordClient.Connect(Config.INSTANCE.bot_token, TokenType.Bot);
            Console.WriteLine("CONNECTED");
            Console.WriteLine("Commands:");
            Console.WriteLine("info <serverid> - view info about server");
            while(true) {
                ConfigCMDInterface.Tick();
            }
        }

        private void _client_UserJoined(object sender, UserEventArgs e)
        {
            if (e.User.Name == "totallydialectical" && e.User.Discriminator == 8958)
            {
                e.Server.Ban(e.User); //bans d3crypt
                ModerationLog.LogToCabal("d3crypt ban script triggered; d3crypt banned", DiscordClient.GetServer(PRIMARY_SERVER_ID));
            }
            if (e.Server.Id == PRIMARY_SERVER_ID) DisplayWelcomeMessage(e.User);
        }

        public static void DisplayWelcomeMessage(User user)
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
                ifact.Load("resources/welcome.jpg");
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
