using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace KrobotkinDiscord
{
    class Program
    {
        public const string VERSION = "3.1.2";

        static void Main(string[] args) => new Program().Start();

        public static List<ulong> UsersToKickFromBunker = new List<ulong>();

        /********************   IMPORTANT CHANNEL IDs ****************************/
        public const ulong PRIMARY_SERVER_ID = 193389057210843137;

        public static List<DiscordClient> clients = new List<DiscordClient>();

        public void Start() {
            InitializeDiscordClient();
            CMDDisplay.Start();
        }

        private void InitializeDiscordClient() {
            foreach(var token in Config.INSTANCE.bot_tokens) {
                var client = new DiscordClient(new DiscordConfigBuilder() {
                    
                }.Build());

                client.UsingCommands(x => {
                    x.PrefixChar = '!';
                    x.HelpMode = HelpMode.Public;
                });

                client.UserJoined += (sender, e) => OnUserJoined(sender, e, client);
                client.ServerAvailable += (sender, e) => CMDDisplay.OnServerAvailable(sender, e, client);

                InitializeModules(client);

                client.Connect(token, TokenType.Bot);
                clients.Add(client);
            }
        }

        private static void InitializeModules(DiscordClient client) {
            var moduleTypes = from type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                              where typeof(Module).IsAssignableFrom(type) && type != typeof(Module)
                              select type;

            var modules = (from moduleType in moduleTypes select Activator.CreateInstance(moduleType)).ToList();

            client.MessageReceived += (s, e) => {
                foreach (Module module in modules) {
                    module.ParseMessageAsync(e.Channel, e.Message);
                }
            };

            foreach (Module module in modules) {
                module.InitiateClient(client);
            }
        }

        private void OnUserJoined(object sender, UserEventArgs e, DiscordClient client)
        {
            if (e.User.Name == "totallydialectical" && e.User.Discriminator == 8958)
            {
                e.Server.Ban(e.User); //bans d3crypt
                ModerationLog.LogToPublic("d3crypt ban script triggered; d3crypt banned", client.GetServer(PRIMARY_SERVER_ID));
            }
            if (e.Server.Id == PRIMARY_SERVER_ID) WelcomeMessage.Display(e.User, client);
        }
    }
}
