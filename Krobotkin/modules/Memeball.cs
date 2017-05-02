using Discord;
using Discord.Commands;
using ImageProcessor.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Krobotkin.modules {
    class Memeball : Module {
        public override void InitiateClient(DiscordClient _client) {
            _client.GetService<CommandService>().CreateCommand("mball")
                .Parameter("Base Image")
                .Parameter("Text")
                .Do(async e => {
                    var channel = e.Channel;
                    var message = e.Message;
                    await channel.SendIsTyping();
                    using (MemoryStream memstream = new MemoryStream()) {

                        try {
                            await ProcessMemeballMeme(message.Text.Substring(7), memstream);
                            await channel.SendFile("meme.png", memstream);
                        } catch (Exception) {
                            await channel.SendMessage("Sorry Dave, I can't do that. :/");
                        }
                    }
                }
            );
        }

        private Task ProcessMemeballMeme(string text, MemoryStream stream) {
            using (ImageProcessor.ImageFactory ifact = new ImageProcessor.ImageFactory()) {
                //TEXT WILL BE 32 CHARACTERS PER LINE, AT A 30 PIXEL HEIGHT IN ARIAL FONT
                //IMAGE ITSELF IS 450 PIXELS HIGH, ADD 34 PX PER LINE OF TEXT
                //TOTAL CANVAS IS 850 PX HIGH,
                String[] words = text.Split(' ');
                ifact.Load($"resources/mball/{words[0]}.png");
                string memetext = "";
                int lines = 0;
                string currentline = "";
                for (int i = 1; i < words.Length; ++i) {
                    string word = words[i];
                    if ((currentline + word).Length >= 32) {
                        memetext += (currentline + "\n");
                        ++lines;
                        currentline = "";
                    }
                    currentline += (word + " ");
                }
                memetext += currentline;
                TextLayer tl = new TextLayer();
                tl.Position = new Point(68, (380 - (34 * lines)));
                tl.FontSize = 30;
                tl.FontFamily = FontFamily.GenericSansSerif;
                tl.Text = memetext;
                tl.FontColor = System.Drawing.Color.Black;
                ifact.Watermark(tl);
                ifact.Crop(new Rectangle(0, (374 - (34 * lines)), 592, 850 - (374 - (34 * lines))));
                ifact.Save(stream);
                return Task.FromResult<object>(null);
            }
        }
    }
}
