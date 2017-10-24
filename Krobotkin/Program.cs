using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;

namespace KrobotkinDiscord {
    class Program {
        public const string VERSION = "3.2.1a";

        public static List<ulong> UsersToKickFromBunker = new List<ulong>();
        public static List<DiscordClient> clients = new List<DiscordClient>();

        /********   IMPORTANT CHANNEL IDs ********/
        public const ulong PRIMARY_SERVER_ID = 193389057210843137;


        static void Main(string[] args) => new Program().Start();

        public void Start() {
            InitializeDiscordClient();
            CMDDisplay.Start();
        }

        private void InitializeDiscordClient() {
            // initialize DiscordClient for each bot token in config
            foreach (var token in Config.INSTANCE.bot_tokens) {
                // initialize client
                var client = new DiscordClient(new DiscordConfigBuilder() {
                    
                }.Build());

                // command settings
                client.UsingCommands(x => {
                    x.PrefixChar = '!';
                    x.HelpMode = HelpMode.Public;
                });

                // set up event handlers
                client.UserJoined += (sender, e) => OnUserJoined(sender, e, client);
                client.ServerAvailable += (sender, e) => CMDDisplay.OnServerAvailable(sender, e, client);

                // initialize modules for client
                InitializeModules(client);

                // connect bot client to discord
                client.Connect(token, TokenType.Bot);
                clients.Add(client);
            }
        }

        private static void InitializeModules(DiscordClient client) {
            var moduleTypes = from type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                              where typeof(Module).IsAssignableFrom(type) && type != typeof(Module)
                              select type;
            var modules = (from moduleType in moduleTypes select Activator.CreateInstance(moduleType)).ToList();

            // set up MessageReceived event handler
            client.MessageReceived += (s, e) => {
                // call ParseMessageAsync callback on modules
                foreach (Module module in modules) {
                    module.ParseMessageAsync(e.Channel, e.Message);
                }
            };

            // call InitiateClient callback on modules
            foreach (Module module in modules) {
                module.InitiateClient(client);
            }
        }

        private void OnUserJoined(object sender, UserEventArgs e, DiscordClient client) {
            if (e.Server.Id == PRIMARY_SERVER_ID) WelcomeMessage.Display(e.User, client);
        }
    }
}
