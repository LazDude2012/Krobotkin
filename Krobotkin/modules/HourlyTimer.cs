using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Timers;

namespace KrobotkinDiscord.Modules {
    class HourlyTimer : Module, IDisposable {
        private Timer Timer = new Timer();

        public void Dispose() {
            Timer.Dispose();
        }

        public override void InitiateClient(DiscordClient _client) {
            Timer.Interval = 3600000;
            Timer.Elapsed += (sender, e) => HourlyTimer_Elapsed(sender, e, _client);
            Timer.AutoReset = true;
            Timer.Start();
        }

        private async void HourlyTimer_Elapsed(object sender, ElapsedEventArgs e, DiscordClient client) {
            // Delete photos (hourly)
            foreach (Channel photoDeleteChannel in (from channel in Config.INSTANCE.deletePhotoChannels select client.GetChannel(channel.channel_id))) {
                if (photoDeleteChannel != null) {
                    Message[] buffer = await photoDeleteChannel.DownloadMessages(100);
                    int messagesRemoved = 0;
                    foreach (Message m in buffer) {
                        if (m.Attachments.Length != 0) {
                            await m.Delete();
                            messagesRemoved++;
                        }
                    }
                    if (messagesRemoved != 0) ModerationLog.LogToPublic($"Hourly purge of selfies removed {messagesRemoved} messages.", photoDeleteChannel.Server);
                }
            }

            // Reminders (hourly)
            foreach (Channel channel in from reminderChannel in Config.INSTANCE.hourlyReminderChannels
                                        select client.GetChannel(reminderChannel.channel_id)
            ) {
                if (channel != null) {
                    await channel.SendMessage(Config.INSTANCE.hourlyReminders[new Random().Next() % Config.INSTANCE.hourlyReminders.Count]);
                }
            }

            // Topic echos (every 12 hours)
            if (e.SignalTime.Hour % 12 == 0) {
                foreach (Channel channel in from echoTopicChannel in Config.INSTANCE.echoTopicChannels
                                            select client.GetChannel(echoTopicChannel.channel_id)
                ) {
                    if (channel != null) {
                        await channel.SendMessage(channel.Topic);
                    }
                }
            }
        }
    }
}
