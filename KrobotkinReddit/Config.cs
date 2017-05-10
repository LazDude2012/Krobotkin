using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace KrobotkinReddit {
    public class Config {
        [XmlIgnore]
        private static Config _instance;
        [XmlIgnore]
        public static Config INSTANCE {
            get {
                if (_instance == null) {
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
        public string username;
        public string password;
        public List<string> subreddits;

        public void Commit() {
            using (FileStream fs = new FileStream("config.xml", FileMode.Create)) {
                XmlSerializer writer = new XmlSerializer(typeof(Config));
                writer.Serialize(fs, Config.INSTANCE);
            }
        }
    }
}
