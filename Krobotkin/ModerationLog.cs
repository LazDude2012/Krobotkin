using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrobotkinDiscord {
    class ModerationLog {
        public static async void LogToPublic(String logMessage, Server server) {
            try {
                Channel cabal = server.GetChannel((from channel in Config.INSTANCE.moderationLogChannels where channel.server_id == server.Id select channel).First().channel_id);
                await cabal.SendMessage($"``` {logMessage} ```");
            }
            catch {
                Console.WriteLine($"[!] {logMessage} on {server}");
            }
        }
    }
}
