using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace KrobotkinDiscord {
    public class Module {
        public virtual void InitiateClient(DiscordClient _client) { }
        public virtual void ParseMessageAsync(Channel channel, Message message) { }
    }
}
