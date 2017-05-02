using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Krobotkin.modules {
    class Aesthetic : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("aes")
                .Parameter("entry", ParameterType.Multiple)
                .Do(e => {
                    e.Channel.SendMessage(ConvertToFullwidth(string.Join(" ", e.Args)));
                }
            );
        }

        private string ConvertToFullwidth(string text) {
            string output = "";

            for (var i = 0; i < text.Length; i++) {
                char normal = text[i];
                try {
                    char latin = Config.INSTANCE.letters[normal];
                    output += latin;
                } catch (KeyNotFoundException) {
                    output += normal;
                }
            }

            return output;
        }
    }
}
