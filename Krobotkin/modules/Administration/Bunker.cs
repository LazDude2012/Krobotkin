using System.Linq;
using Discord;
using Discord.Commands;

namespace KrobotkinDiscord.Modules.Administration {
    public class Bunker : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("approve")
                .Parameter("user")
                .Description("FC Bunker Entrance only; approves user for FULLCOMM entry. Gives them the Gif and the invite, kicks them next hourly cycle.")
                .Do(async e => {
                    // if bunker entrance server
                    if (e.Server.Id == 248993874666586142) {
                        Server fullcomm = _client.GetServer(Program.PRIMARY_SERVER_ID);
                        await e.Channel.SendIsTyping();
                        //await e.Channel.SendFile(@"C:\Users\lazdu\OneDrive\FULLCOMM SHARED\commie memes\approved.gif");
                        Invite inv = await fullcomm.CreateInvite(maxUses: 1);
                        await e.Message.MentionedUsers.First().SendMessage(inv.Url);
                        await e.User.SendMessage(inv.Url);
                        Program.UsersToKickFromBunker.Add(e.Message.MentionedUsers.First().Id);
                    }
                }
            );
        }
    }
}
