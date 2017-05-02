using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;

namespace Krobotkin
{
    class Krobotkin
    {
        public const string VERSION = "3.0";

        static void Main(string[] args) => new Krobotkin().Start();

        public static DiscordClient DiscordClient;

        public static List<ulong> UsersToKickFromBunker = new List<ulong>();

        /********************   IMPORTANT CHANNEL IDs ****************************/
        public const ulong PRIMARY_SERVER_ID = 193389057210843137;

        public void Start() {
            DiscordClient = new DiscordClient();

            DiscordClient.UsingCommands(x =>
            {
                x.PrefixChar = '!';
                x.HelpMode = HelpMode.Public;
            });

            DiscordClient.UserJoined += _client_UserJoined;
            DiscordClient.ServerAvailable += CMDDisplay.OnServerAvailableAsync;

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

            foreach(EchoCommand ec in Config.INSTANCE.echoCommands)
            {
                DiscordClient.GetService<CommandService>().CreateCommand(ec.challenge)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(ec.response);
                    });
            }

            DiscordClient.Connect(Config.INSTANCE.bot_token, TokenType.Bot);
            CMDDisplay.Start();
        }

        private void _client_UserJoined(object sender, UserEventArgs e)
        {
            if (e.User.Name == "totallydialectical" && e.User.Discriminator == 8958)
            {
                e.Server.Ban(e.User); //bans d3crypt
                ModerationLog.LogToCabal("d3crypt ban script triggered; d3crypt banned", DiscordClient.GetServer(PRIMARY_SERVER_ID));
            }
            if (e.Server.Id == PRIMARY_SERVER_ID) WelcomeMessage.Display(e.User);
        }
    }
}
