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
        internal static async Task<StatusResponse> Tweet(string text, long replyid) =>
            await Token.Statuses.UpdateAsync(status => text, in_reply_to_status_id => replyid);

        internal static async Task<StatusResponse> RequestTweetInfomation(long targetid) =>
            await Token.Statuses.ShowAsync(id => targetid);

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
        internal static long QuestionStatusId { get; set; }
        internal static long LatestReplyStatusId { get; set; }
        private static List<string> SelfIntroductionAnswers { get; set; } = new List<string>();
        private static int SendedQuestionNumber = 0;

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
                        selfintroductionquestion = selfintroductionquestion.Substring(0, selfintroductionquestion.Length - 1);
                        QuestionStatusId = TwitterAPI.Tweet(selfintroductionquestion).Result.Id;
                        LatestReplyStatusId = QuestionStatusId;
                        istweeted = true;
                    }
                }
            }
        }
        internal static void TweetSelfIntroductionAnswers()
        {
            SelfIntroductionAnswers[FavCount - 1] = $"{FavCount}:{SelfIntroductionAnswers[FavCount - 1]}";
            LatestReplyStatusId = TwitterAPI.Tweet(SelfIntroductionAnswers[FavCount - 1], LatestReplyStatusId).Result.Id;
            SendedQuestionNumber++;
        }
        internal static void OnEventReceived(EventMessage message)
        {
            int currentFav = (int)TwitterAPI.RequestTweetInfomation(QuestionStatusId).Result.FavoriteCount;
            bool isFavCountChanged = FavCount < currentFav ? true : false;
            FavCount = currentFav;
            if (isFavCountChanged && FavCount <= SelfIntroductionAnswers.Count && FavCount > SendedQuestionNumber && IsTargetEvent(message))
            {
                //FavCount = (int)message.TargetStatus.FavoriteCount;
                TweetSelfIntroductionAnswers();
            }
        }
        private static bool IsTargetEvent(EventMessage message) =>
           message.Event == EventCode.Favorite && message.TargetStatus.Id == QuestionStatusId
                ? true
                : false;
    }
}