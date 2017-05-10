using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.IO;
using System.Text.RegularExpressions;

namespace KrobotkinDiscord.Modules {
    class Corpus : Module {
        public override void InitiateClient(DiscordClient _client) {
            foreach(var authorDirectory in Directory.EnumerateDirectories("resources/corpus")) {
                var authorName = authorDirectory.Substring("resources/corpus\\".Length);
                _client.GetService<CommandService>().CreateCommand(authorName)
                    .Parameter("phrase", ParameterType.Multiple)
                    .Do(async e => {
                        var phrase = string.Join(" ", e.Args);
                        var response = FindCorpusText(phrase, authorName, false);

                        if (response == "") await e.Channel.SendMessage("None found.");
                        else await e.Channel.SendMessage(response);
                    }
                );
            }
        }

        private string FindCorpusText(string text, string author, bool limitOne) {
            string output = "";

            if (Directory.Exists("resources/corpus") && Directory.Exists("resources/corpus/" + author)) {
                Random rnd = new System.Random();
                string[] files = Directory.GetFiles("resources/corpus/" + author);
                files = files.OrderBy(x => rnd.Next()).ToArray();
                int resultsTruncated = 0;
                foreach (string file in files) {
                    string fullFileText = File.ReadAllText(file);
                    char[] fullStop = { '.', '!', '?', ';' };
                    string[] fileLines = fullFileText.Split(fullStop);
                    fileLines = fileLines.OrderBy(x => rnd.Next()).ToArray();
                    foreach (string line in fileLines) {
                        if (Regex.IsMatch(line, text, RegexOptions.IgnoreCase)) {
                            if (output.Length > 900) {
                                ++resultsTruncated;
                            } else {
                                output += file.Substring(file.LastIndexOfAny(new char[] { '\\', '/' }) + 1, (file.LastIndexOf(".", StringComparison.Ordinal) - file.LastIndexOfAny(new char[] { '\\', '/' }))) + ": " + line.Trim(' ', '\r', '\n', '\t') + "\n";
                                if (limitOne) {
                                    break;
                                }
                            }

                        }

                    }
                    if (limitOne) {
                        break;
                    }


                }
                if (resultsTruncated != 0) output += $"{resultsTruncated} more results were found but omitted for space.";
            } else {
                output = "Sorry, Krobotkin has no text for " + author;
            }
            return output;
        }
    }
}
