using Discord;
using ImageProcessor.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrobotkinDiscord {
    public static class WelcomeMessage {
        public static void Display(User user, DiscordClient client) {
            byte[] avatar = null;
            using (var wc = new System.Net.WebClient()) {
                avatar = (user.AvatarUrl == null) ? null : wc.DownloadData(user.AvatarUrl);
                if (avatar == null) {
                    client.GetChannel(
                        (from channel in Config.INSTANCE.primaryChannels where channel.server_id == user.Server.Id select channel.channel_id).First()
                    ).SendMessage("Welcome new comrade" + user.Mention);

                    return;
                }
            }
            var astream = new MemoryStream(avatar);
            Image ai = Image.FromStream(astream);
            var outstream = new MemoryStream();
            using (var ifact = new ImageProcessor.ImageFactory()) {
                //159,204 image size 283x283
                ImageLayer ilay = new ImageLayer() {
                    Image = ai,
                    Size = new Size(283, 283),
                    Position = new Point(159, 204)
                };
                ifact.Load("resources/welcome.jpg");
                ifact.Overlay(ilay);
                System.Drawing.Color yellow = System.Drawing.Color.FromArgb(208, 190, 25);
                TextLayer uname = new TextLayer() { Position = new Point(108, 512), FontFamily = FontFamily.GenericSansSerif, FontSize = 30, Text = user.Nickname, FontColor = yellow };
                ifact.Watermark(uname);
                ifact.Save(outstream);
            }
            Channel general = client.GetChannel((from channel in Config.INSTANCE.primaryChannels where channel.server_id == user.Server.Id select channel.channel_id).First());
            general.SendMessage("Welcome new comrade " + user.Mention);
            general.SendFile("welcome.jpg", outstream);
            ModerationLog.LogToPublic($"User {user} joined.", client.GetServer(user.Server.Id));
        }
    }
}
