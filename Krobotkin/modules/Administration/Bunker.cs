using System.Linq;
using Discord;
using Discord.Commands;

namespace Krobotkin.modules.Administration {
    public class Bunker : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("approve")
                .Parameter("user")
                .Description("FC Bunker Entrance only; approves user for FULLCOMM entry. Gives them the Gif and the invite, kicks them next hourly cycle.")
                .Do(async e => {
                    if (e.Server.Id == 248993874666586142) // bunker entrance
                    {
                        Server fullcomm = _client.GetServer(Krobotkin.PRIMARY_SERVER_ID);
                        await e.Channel.SendIsTyping();
                        //await e.Channel.SendFile(@"C:\Users\lazdu\OneDrive\FULLCOMM SHARED\commie memes\approved.gif");
                        Invite inv = await fullcomm.CreateInvite(maxUses: 1);
                        await e.Message.MentionedUsers.First().SendMessage(inv.Url);
                        await e.User.SendMessage(inv.Url);
                        Krobotkin.UsersToKickFromBunker.Add(e.Message.MentionedUsers.First().Id);
                    }
                }
            );
        }
    }
}
