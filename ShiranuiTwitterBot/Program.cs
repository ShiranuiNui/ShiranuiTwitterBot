using System;
using CoreTweet;

namespace ShiranuiTwitterBot
{
    class Program
    {
        static void Main(string[] args)
        {
            //var tokens = Tokens.Create();
            tokens.Statuses.UpdateAsync(status => "ShiranuiTest").Wait();
        }
    }
}