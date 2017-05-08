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
        static List<string> hiddenPosts = new List<string>();
        static void Main(string[] args) {
            Console.WriteLine("Starting");
            var reddit = new Reddit();

            AuthenticatedUser user = null;
            while (user == null) {
                try {
                    user = reddit.LogIn(Config.INSTANCE.username, Config.INSTANCE.password);
                } catch (Exception e) {
                    Console.WriteLine("Failed to log in:" + e);
                }
            }
           
            
            Console.WriteLine("Logged in");

            var subreddits = new List<Subreddit>();

            var retry = false;
            do {
                retry = false;
                foreach (var subName in Config.INSTANCE.subreddits) {
                    try {
                        subreddits.Add(reddit.GetSubreddit(subName));
                    } catch (Exception e) {
                        Console.WriteLine("Failed to retrieve subreddit information: " + e);
                        retry = true;
                        break;
                    }
                }
            } while (retry);
            

            foreach(Subreddit subreddit in subreddits) {
                subreddit.Subscribe();
            }

            Console.WriteLine("Looping...");

            ThreadPool.SetMaxThreads(3, 3);

            while (true) {
                var threads = new List<Thread>();
                foreach (Subreddit subreddit in subreddits) {
                    threads.Add(new Thread(new ThreadStart(() => {
                        foreach (var post in subreddit.New.Take(10)) {
                            if (hiddenPosts.Contains(post.Id)) {
                                continue;
                            }
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
                                    try {
                                        var response = new KrobotkinOCR().GetTextFromImage(post.Url.AbsoluteUri);
                                        if (response.Text.Length == 0) {
                                            Console.WriteLine($"{ subreddit.Name}: Skip empty string");
                                            hiddenPosts.Add(post.Id);
                                        } else {
                                            if (response.Confidence > 0.74) {
                                                var fullComment = 
$@"    {response.Text.Replace("\n", "\n\n    ")}

^I'm ^a ^bot ^that ^tries ^to ^help ^out ^our ^blind ^comrades ^by ^interpreting ^text ^from ^an ^image

If that didn't make any sense, please reply with a correction. See [this comment](https://www.reddit.com/r/me_irl/comments/698l3f/meirl/dh4maww/?context=3) for a sample correction

^^^(Confidence:{response.Confidence})";
                                                post.Comment(fullComment);
                                                Console.WriteLine($"{subreddit.Name}: Posted \n" + fullComment + "\n=============");
                                                hiddenPosts.Add(post.Id);
                                            } else {
                                                Console.WriteLine($"{subreddit.Name}: Skip Non-confident");
                                                hiddenPosts.Add(post.Id);
                                            }
                                        }
                                    } catch (RateLimitException e) {
                                        Console.WriteLine($"{subreddit.Name}: Skip Rate limited" + e.TimeToReset);
                                        Thread.Sleep(e.TimeToReset.Add(new TimeSpan(0, 0, 5))); //Wait 5 extra seconds
                                    } catch {
                                        Console.WriteLine($"{subreddit.Name}: Skip nothing found");
                                        hiddenPosts.Add(post.Id);
                                    }
                                } else {
                                    Console.WriteLine($"{subreddit.Name}: Skip already posted");
                                    hiddenPosts.Add(post.Id);
                                }
                            } else {
                                Console.WriteLine($"{subreddit.Name}: Skip non-image");
                                hiddenPosts.Add(post.Id);
                            }
                        }
                    })));
                }
                threads.Add(new Thread(new ThreadStart(() => {
                    foreach (var message in user.UnreadMessages) {
                        if(message.GetType() == typeof(Comment)) {
                            var reply = (Comment)message;
                            reply.Body = reply.Body.Replace("&gt;", ">");

                            var acceptableStarts = new string[] {
                                "Correction: \n\n>",
                                "Correction \n\n>",
                                "Correction:\n\n>",
                                "Correction\n\n>"
                            };

                            foreach(var start in acceptableStarts) {
                                if (reply.Body.StartsWith(start)) {
                                    var correction = reply.Body.Substring(start.Length).Split(new string[] { "\n\n>" }, StringSplitOptions.None);

                                    for (int i = 0; i < correction.Length; i++) {
                                        correction[i] = correction[i].Trim();
                                    }

                                    reply.SetAsRead();
                                    reply.SetVote(VotableThing.VoteType.Upvote);
                                    reply.Reply("[Thank you for the correction! You have been awarded 1 Reddit Labour Voucher](https://i.redditmedia.com/9ZR9TMffBhjkxP2BGA5ry6AqN2tUQ95oS4SVm0-Ipfw.jpg?w=1024&s=b6c4ddb97ac34d2b14ece2c013a47b34)");

                                    var parentComment = reply.Parent;

                                }
                            }
                        }
                    }
                })));
                foreach (Thread thread in threads) {
                    thread.Start();
                }
                foreach (Thread thread in threads) {
                    thread.Join();
                }
            }
        }
    }
}
