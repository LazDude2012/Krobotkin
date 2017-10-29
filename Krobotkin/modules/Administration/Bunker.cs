using System.Linq;
using Discord;
using Discord.Commands;

namespace KrobotkinDiscord.Modules.Administration {
    public class Bunker : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("approve")
                .Parameter("user")
                .Description("Bunker Entrance server only; approves user for entry to main server. Gives the invite, kicks them next hourly cycle.")
                .Do(async e => {
                    // if bunker entrance server
                    if (e.Server.Id == 248993874666586142) {
                        Server main_server = _client.GetServer(Program.PRIMARY_SERVER_ID);
                        User invUser = e.Message.MentionedUsers.FirstOrDefault();
                        if (invUser == null) return;

                        // generate invite link
                        Invite inv = await main_server.CreateInvite(maxUses: 1);
                        string invUrl = inv.Url.Replace("g//", "g/");

                        // send invite messages
                        await invUser.SendMessage(invUrl);
                        await e.User.SendMessage(invUrl);
                        await e.Channel.SendMessage($"{invUser.Mention} - server invite sent, please check your DMs.");

                        // not currently implemented
                        //Program.UsersToKickFromBunker.Add(e.Message.MentionedUsers.First().Id);
                    }
                }
            );
        }
    }
}
