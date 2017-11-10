using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace KrobotkinDiscord {
    class ImageDeleter : Module, IDisposable {
        public List<Timer> Timers = new List<Timer>();

        public void Dispose() {
            foreach(Timer timer in Timers) {
                timer.Dispose();
            }
        }

        public override void ParseMessageAsync(Channel channel, Message message) {
            if (message.Attachments.Count() > 0) {
                foreach (var configChannel in Config.INSTANCE.deletePhotoChannels) {
                    if (message.Channel.Id == configChannel.channel_id && message.Server.Id == configChannel.server_id) {
                        var timer = new Timer();
                        timer.Interval = 1000 * 60 * 60; // 1 hour
                        timer.Elapsed += async (sender, e) => {
                            await message.Delete();
                            Timers.Remove(timer);
                            timer.Dispose();
                        };
                        timer.Start();
                        Timers.Add(timer);
                    }
                }
            }
        }
    }
}
