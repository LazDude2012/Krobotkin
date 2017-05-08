using Discord;

namespace KrobotkinDiscord.Modules {
    class Blacklist : Module {
        public override async void ParseMessage(Channel channel, Message message) {
            foreach (string word in Config.INSTANCE.Blacklist) {
                if (message.Text.ToLower().Contains(word) && !message.User.IsBot && Config.INSTANCE.GetPermissionLevel(message.User, message.Server) < 2) {
                    ModerationLog.LogToPublic($"User {message.User} used blacklisted word {word} in channel #{channel.Name}. Message was as follows: \n {message}", message.Server);
                    await channel.DeleteMessages(new Message[] { message });
                    await channel.SendMessage("``` Message redacted for the sake of the Motherland. ```");
                    break;
                }
            }
        }
    }
}
