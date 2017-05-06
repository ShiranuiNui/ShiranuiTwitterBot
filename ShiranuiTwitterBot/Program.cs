using System;
using System.IO;
using System.Threading.Tasks;
using System.Reactive.Linq;
using CoreTweet.Streaming;
using CoreTweet;
using System.Collections.Generic;

namespace ShiranuiTwitterBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().Wait();
            Console.ReadLine();
        }
        private static async Task MainAsync()
        {
            TwitterAPI.ReadTokens();
            SelfIntroduction.SetSelfIntroductionInfomation();
            //var result = Tweet("ShiranuiTest").Result;
            //Tweet("ReplyTest", result.Id);
            TwitterAPI.ReadTimelines();
        }
    }
    static class TwitterAPI
    {
        private static Tokens Token = null;
        internal static void ReadTokens()
        {
            var tokenTextArray = File.ReadAllLines(@"AppConfig\config.txt");
            Token = Tokens.Create(tokenTextArray[0], tokenTextArray[1], tokenTextArray[2], tokenTextArray[3]);
        }
        internal static async Task<StatusResponse> Tweet(string text) =>
            await Token.Statuses.UpdateAsync(status => text);
        internal static async void Tweet(string text, long replyid) =>
            await Token.Statuses.UpdateAsync(status => text, in_reply_to_status_id => replyid);

        internal static async void ReadTimelines()
        {
            var stream = Token.Streaming.UserAsObservable().Publish();
            stream.OfType<EventMessage>().Subscribe(message => SelfIntroduction.OnEventReceived(message));
            var disposable = stream.Connect();
        }
    }
    static class SelfIntroduction
    {
        internal static int FavCount = 0;
        internal static long TargetStatusId { get; set; }
        private static List<string> SelfIntroductionAnswers { get; set; } = new List<string>();

        internal static void SetSelfIntroductionInfomation()
        {
            string selfintroductionquestion = null;
            bool istweeted = false;
            //foreach (string line in File.ReadLines(@"AppConfig\config.txt"))
            using (var fs = new FileStream(@"AppConfig\selfintroduction.txt", FileMode.Open))
            using (var reader = new StreamReader(fs))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!istweeted && line != "")
                    {
                        selfintroductionquestion += line + "\n";
                    }
                    if (istweeted)
                    {
                        SelfIntroductionAnswers.Add(line);
                    }
                    if (line == "")
                    {
                        selfintroductionquestion = selfintroductionquestion.Substring(0, selfintroductionquestion.Length - 2);
                        TargetStatusId = TwitterAPI.Tweet(selfintroductionquestion).Result.Id;
                        istweeted = true;
                    }
                }
            }
        }
        internal static void TweetSelfIntroductionAnswers()
        {
            TwitterAPI.Tweet(SelfIntroductionAnswers[FavCount], TargetStatusId);
            FavCount++;
        }
        internal static void OnEventReceived(EventMessage message)
        {
            if (IsTargetEvent(message))
            {
                TweetSelfIntroductionAnswers();
            }
        }
        private static bool IsTargetEvent(EventMessage message) =>
           message.Event == EventCode.Favorite && message.TargetStatus.Id == TargetStatusId
                ? true
                : false;
    }
}