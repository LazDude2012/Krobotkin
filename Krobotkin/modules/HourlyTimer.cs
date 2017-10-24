using System;
using System.Linq;
using System.Timers;
using Discord;
using Discord.Commands;


namespace KrobotkinDiscord.Modules {
    class HourlyTimer : Module, IDisposable {
        private Timer Timer = new Timer();

        public void Dispose() {
            Timer.Dispose();
        }

        public override void InitiateClient(DiscordClient _client) {
            // set up hourly timer
            Timer.Interval = 3600000;
            Timer.Elapsed += (sender, e) => HourlyTimer_Elapsed(sender, e, _client);
            Timer.AutoReset = true;
            Timer.Start();

            // set up commands
            _client.GetService<CommandService>().CreateGroup("reminders", egp => {
                egp.CreateCommand("add")
                .Description("Add an hourly reminder.")
                .Parameter("content", ParameterType.Multiple)
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    String content = String.Join(" ", e.Args);
                    if (content.Length == 0) {
                        e.Channel.SendMessage($"Reminder cannot be empty.");
                        return;
                    }

                    // add reminder
                    Config.INSTANCE.hourlyReminders.Add(content);
                    Config.INSTANCE.Commit();
                    e.Channel.SendMessage($"Added hourly reminder:\n`{content}`");
                    ModerationLog.LogToPublic($"User {e.User.Name} added hourly reminder: {content}", e.Server);
                });

                egp.CreateCommand("remove")
                .Description("Remove an hourly reminder by ID number.")
                .Parameter("id")
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    int index = Int32.Parse(e.GetArg("id")); // index = [1 ... n]

                    // check index bounds
                    if (index <= 0 || index > Config.INSTANCE.hourlyReminders.Count) {
                        e.Channel.SendMessage($"Invalid reminder ID #{index}");
                        return;
                    }

                    // remove reminder
                    String reminder = Config.INSTANCE.hourlyReminders.ElementAt(index - 1);
                    Config.INSTANCE.hourlyReminders.RemoveAt(index - 1);
                    Config.INSTANCE.Commit();
                    e.Channel.SendMessage($"Removed hourly reminder:\n`{reminder}`");
                    ModerationLog.LogToPublic($"User {e.User.Name} removed hourly reminder: {reminder}", e.Server);
                });

                egp.CreateCommand("edit")
                .Description("Edit an hourly reminder by ID number.")
                .Parameter("id")
                .Parameter("content", ParameterType.Multiple)
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    int index = Int32.Parse(e.GetArg("id")); // index = [1 ... n]
                    String content = String.Join(" ", e.Args.Skip(1));

                    // check index bounds
                    if (index <= 0 || index > Config.INSTANCE.hourlyReminders.Count) {
                        e.Channel.SendMessage($"Invalid reminder ID #{index}");
                        return;
                    }

                    // edit reminder
                    String reminder = Config.INSTANCE.hourlyReminders.ElementAt(index - 1);
                    Config.INSTANCE.hourlyReminders[index - 1] = content;
                    Config.INSTANCE.Commit();
                    e.Channel.SendMessage($"Edited hourly reminder #{index}\n```before:\n{reminder}``````after:\n{content}```");
                    ModerationLog.LogToPublic($"User {e.User.Name} edited hourly reminder: {reminder}", e.Server);
                });

                egp.CreateCommand("print")
                .Description("Print all hourly reminders by ID number.")
                .Do(e => {
                    // check permissions
                    if (Config.INSTANCE.GetPermissionLevel(e.User, e.Server) < 2) {
                        e.Channel.SendMessage("Sorry, you don't have permission to do that.");
                        return;
                    }

                    String rstring = "";
                    for (int i = 0; i < Config.INSTANCE.hourlyReminders.Count; i++) {
                        String reminder = Config.INSTANCE.hourlyReminders.ElementAt(i);
                        rstring += $"{i+1}. {reminder}\n";
                    }

                    e.Channel.SendMessage($"Hourly reminders:\n{rstring}");
                });
            });
        }

        private async void HourlyTimer_Elapsed(object sender, ElapsedEventArgs e, DiscordClient client) {

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
