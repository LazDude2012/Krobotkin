using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using System.Text;
using System.Diagnostics;

namespace KrobotkinDiscord
{
    class Program
    {
        public const string VERSION = "3.0a";

        static void Main(string[] args) => new Program().Start();

        public static DiscordClient DiscordClient;

        public static List<ulong> UsersToKickFromBunker = new List<ulong>();

        /********************   IMPORTANT CHANNEL IDs ****************************/
        public const ulong PRIMARY_SERVER_ID = 193389057210843137;

        public void Start() {
            InitializeDiscordClient();
            CMDDisplay.Start();
        }

        private void InitializeDiscordClient() {
            DiscordClient = new DiscordClient();

            DiscordClient.UsingCommands(x => {
                x.PrefixChar = '!';
                x.HelpMode = HelpMode.Public;
            });

            DiscordClient.UserJoined += OnUserJoined;
            DiscordClient.ServerAvailable += CMDDisplay.OnServerAvailableAsync;

            InitializeModules();

            foreach (EchoCommand ec in Config.INSTANCE.echoCommands) {
                DiscordClient.GetService<CommandService>().CreateCommand(ec.challenge)
                    .Do(async e => {
                        await e.Channel.SendMessage(ec.response);
                    });
            }

            DiscordClient.Connect(Config.INSTANCE.bot_token, TokenType.Bot);
        }

        private static void InitializeModules() {
            var moduleTypes = from type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                              where typeof(Module).IsAssignableFrom(type) && type != typeof(Module)
                              select type;

            var modules = (from moduleType in moduleTypes select Activator.CreateInstance(moduleType)).ToList();

            DiscordClient.MessageReceived += (s, e) => {
                foreach (Module module in modules) {
                    module.ParseMessage(e.Channel, e.Message);
                }
            };

            foreach (Module module in modules) {
                module.InitiateClient(DiscordClient);
            }
        }

        private void OnUserJoined(object sender, UserEventArgs e)
        {
            if (e.User.Name == "totallydialectical" && e.User.Discriminator == 8958)
            {
                e.Server.Ban(e.User); //bans d3crypt
                ModerationLog.LogToPublic("d3crypt ban script triggered; d3crypt banned", DiscordClient.GetServer(PRIMARY_SERVER_ID));
            }
            if (e.Server.Id == PRIMARY_SERVER_ID) WelcomeMessage.Display(e.User);
        }
    }
}
