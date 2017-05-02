using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Timers;

namespace Krobotkin.Modules {
    class HourlyTimer : Module {
        private Timer Timer = new Timer();

        public override void InitiateClient(DiscordClient _client) {
            Timer.Interval = 3600000;
            Timer.Elapsed += HourlyTimer_Elapsed;
            Timer.AutoReset = true;
            Timer.Start();
        }

        private async void HourlyTimer_Elapsed(object sender, ElapsedEventArgs e) {
            foreach (Channel photoDeleteChannel in (from channel in Config.INSTANCE.deletePhotoChannels select Krobotkin.DiscordClient.GetChannel(channel.channel_id))) {
                if (photoDeleteChannel != null) {
                    Message[] buffer = await photoDeleteChannel.DownloadMessages(100);
                    int messagesRemoved = 0;
                    foreach (Message m in buffer) {
                        if (m.Attachments.Length != 0) {
                            await m.Delete();
                            messagesRemoved++;
                        }
                    }
                    if (messagesRemoved != 0) ModerationLog.LogToCabal($"Hourly purge of selfies removed {messagesRemoved} messages.", photoDeleteChannel.Server);
                }

                foreach (Channel channel in from reminderChannel in Config.INSTANCE.hourlyReminderChannels
                                            select Krobotkin.DiscordClient.GetChannel(reminderChannel.channel_id)
                ) {
                    if (channel != null) {
                        await channel.SendMessage(Config.INSTANCE.hourlyReminders[new Random().Next() % Config.INSTANCE.hourlyReminders.Count]);
                    }
                }
            }
        }
    }
}
