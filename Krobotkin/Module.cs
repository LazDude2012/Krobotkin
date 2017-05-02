using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Krobotkin {
    public class Module {
        public virtual void InitiateClient(DiscordClient _client) { }
        public virtual void ParseMessage(Channel channel, Message message) { }
    }
}
