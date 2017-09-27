using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace KrobotkinDiscord {
    public class Config
    {
        [XmlIgnore]
        private static Config _instance;
        [XmlIgnore]
        public static Config INSTANCE {
            get {
                if(_instance == null) {
                    if (File.Exists("config.xml")) {
                        using (FileStream fs = new FileStream("config.xml", FileMode.OpenOrCreate)) {
                            XmlSerializer reader = new XmlSerializer(typeof(Config));
                            _instance = (Config)reader.Deserialize(fs);
                        }
                    } else {
                        _instance = new Config();
                        Console.WriteLine("Did not find config, generating empty one");
                    }
                }
                return _instance;
            }
        }
        public List<String> Blacklist;
        public List<ConfigRole> roles;
        public List<ConfigChannel> primaryChannels;
        public List<ConfigChannel> hourlyReminderChannels;
        public List<ConfigChannel> moderationLogChannels;
        public List<ConfigChannel> deletePhotoChannels;
        public List<ConfigChannel> echoTopicChannels;
        public List<ConfigUser> sleepedUsers;
        public List<EchoCommand> echoCommands;
        public List<String> hourlyReminders;
        public List<String> bot_tokens;

        public void Commit() {
            using (FileStream fs = new FileStream("config.xml", FileMode.Create)) {
                XmlSerializer writer = new XmlSerializer(typeof(Config));
                writer.Serialize(fs, Config.INSTANCE);
            }
        }

        public int GetPermissionLevel(User user, Server server) {
            if (user == null || server == null) return -1; //Message was deleted too fast to grab the user usually
            if (user.Id == 159017676662898696) return 3; //laz always gets highest powers
            if (user.Id == 312809122439626752) return 3; //gigi too thanks
            else {
                int permLevel = 0;
                foreach (ConfigRole r in roles) {
                    if (server.GetRole(r.role_id) == null) continue;
                    else {
                        if (user.HasRole(server.GetRole(r.role_id)) && r.trust_level > permLevel)
                            permLevel = r.trust_level;
                    }
                }
                if(permLevel < 2 && sleepedUsers.Contains(new ConfigUser() { user_id = user.Id })) {
                    return -1;
                }
                return permLevel;
            }
        }

        [XmlIgnore]
        #region unicode_dictionary
        public Dictionary<char, char> letters = new Dictionary<char, char>()
            {
                {'\u0020', '\u3000'},
                {'\u0041', '\uff21'},
                {'\u0042', '\uff22'},
                {'\u0043', '\uff23'},
                {'\u0044', '\uff24'},
                {'\u0045', '\uff25'},
                {'\u0046', '\uff26'},
                {'\u0047', '\uff27'},
                {'\u0048', '\uff28'},
                {'\u0049', '\uff29'},
                {'\u004a', '\uff2a'},
                {'\u004b', '\uff2b'},
                {'\u004c', '\uff2c'},
                {'\u004d', '\uff2d'},
                {'\u004e', '\uff2e'},
                {'\u004f', '\uff2f'},
                {'\u0050', '\uff30'},
                {'\u0051', '\uff31'},
                {'\u0052', '\uff32'},
                {'\u0053', '\uff33'},
                {'\u0054', '\uff34'},
                {'\u0055', '\uff35'},
                {'\u0056', '\uff36'},
                {'\u0057', '\uff37'},
                {'\u0058', '\uff38'},
                {'\u0059', '\uff39'},
                {'\u005a', '\uff3a'},
                {'\u0061', '\uff41'},
                {'\u0062', '\uff42'},
                {'\u0063', '\uff43'},
                {'\u0064', '\uff44'},
                {'\u0065', '\uff45'},
                {'\u0066', '\uff46'},
                {'\u0067', '\uff47'},
                {'\u0068', '\uff48'},
                {'\u0069', '\uff49'},
                {'\u006a', '\uff4a'},
                {'\u006b', '\uff4b'},
                {'\u006c', '\uff4c'},
                {'\u006d', '\uff4d'},
                {'\u006e', '\uff4e'},
                {'\u006f', '\uff4f'},
                {'\u0070', '\uff50'},
                {'\u0071', '\uff51'},
                {'\u0072', '\uff52'},
                {'\u0073', '\uff53'},
                {'\u0074', '\uff54'},
                {'\u0075', '\uff55'},
                {'\u0076', '\uff56'},
                {'\u0077', '\uff57'},
                {'\u0078', '\uff58'},
                {'\u0079', '\uff59'},
                {'\u007a', '\uff5a'}
            };
        #endregion unicode_dictionary
    }
    public class ConfigRole
    {
        public string name;
        public ulong server_id; 
        public ulong role_id;
        public int trust_level; //0 for users, 1 for trusted users, 2 for mods
    }
    public class ConfigChannel {
        public ulong server_id;
        public ulong channel_id;
    }
    public class ConfigUser : IEquatable<ConfigUser> {
        public ulong user_id;

        public bool Equals(ConfigUser other) {
            return other.user_id == this.user_id;
        }
    }
    public class EchoCommand : IEquatable<EchoCommand>
    {
        public string challenge;
        public string response;
        public ulong server_id;

        public bool Equals(EchoCommand other) {
            return challenge == other.challenge && server_id == other.server_id;
        }
    }
}
