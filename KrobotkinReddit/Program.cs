using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KrobotkinReddit {
    class Program {
        static void Main(string[] args) {
            var reddit = new Reddit();
            var user = reddit.LogIn("KrobotkinOCR", "#Gigimoi00");

            var subredditNames = new string[] { "/r/FullCommunism", "/r/FullFreeTerritory", "/r/LateStageCapitalism", "/r/wholesomecommiememes" };

            var subreddits = new List<Subreddit>();

            foreach(var subName in subredditNames) {
                subreddits.Add(reddit.GetSubreddit(subName));
            }

            foreach(Subreddit subreddit in subreddits) {
                subreddit.Subscribe();
            }
            while(true) {
                foreach (Subreddit subreddit in subreddits) {
                    foreach (var post in subreddit.New.Take(20)) {
                        if (post.Url.AbsoluteUri.EndsWith(".png") ||
                            post.Url.AbsoluteUri.EndsWith(".jpg") ||
                            post.Url.AbsoluteUri.EndsWith(".tiff") ||
                            post.Url.AbsoluteUri.EndsWith(".bmp")
                        ) {
                            var alreadyDone = false;
                            foreach (var comment in post.Comments) {
                                if (comment.Author == "KrobotkinOCR") {
                                    alreadyDone = true;
                                }
                            }
                            if (!alreadyDone) {
                                var disclaimer = "Beep Boop I'm a bot that tries to help out our blind comrades by interpreting text from an image\n\n    ";
                                try {
                                    var response = new KrobotkinOCR().GetTextFromImage(post.Url.AbsoluteUri);
                                    if (response.Text.Length == 0) {
                                        Console.WriteLine("Skip empty string");
                                    } else {
                                        if(response.Confidence > 0.74) {
                                            Console.WriteLine("Posting...");
                                            var fullComment = disclaimer + response.Text.Replace("\n", "\n    ") + "\n\nIf that didn't make any sense, please pm me letting me know. Thanks!\n\n(Confidence: " + response.Confidence + ")";
                                            post.Comment(fullComment);
                                            Console.WriteLine("Posted \n" + fullComment + "\n=============");
                                        }
                                        else {
                                            Console.WriteLine("Skip Non-confident");
                                        }
                                    }
                                } catch (RateLimitException e) {
                                    Console.WriteLine("Skip Rate limited" + e.TimeToReset);
                                    Thread.Sleep(e.TimeToReset.Add(new TimeSpan(0, 0, 5))); //Wait 5 extra seconds
                                } catch {
                                    Console.WriteLine("Skip nothing found");
                                }
                            } else {
                                Console.WriteLine("Skip already posted");
                            }
                        } else {
                            Console.WriteLine("Skip non-image");
                        }
                    }
                }
            }
        }
    }
}
