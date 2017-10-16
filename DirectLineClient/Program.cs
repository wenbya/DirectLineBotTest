namespace DirectLineSampleClient
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.DirectLine;
    using Models;
    using Newtonsoft.Json;

    public class Program
    {
        private static string directLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private static string botId = ConfigurationManager.AppSettings["BotId"];
        private static string fromUser = "wenbo";

        public static void Main(string[] args)
        {
            StartBotConversation().Wait();
        }

        private static async Task StartBotConversation()
        {
            // Connect to the DirectLine service
            DirectLineClient client = new DirectLineClient(directLineSecret);
            
            // Start a Conversation
            var conversation = await client.Conversations.StartConversationAsync();

            // Messages from the bot are continually polled from the API in another Thread in the ReadBotMessageAsync method
            // The GetActivitiesAsynv method which retrieves conversation message newer than the stored watermark
            new System.Threading.Thread(async () => await ReadBotMessagesAsync(client, conversation.ConversationId)).Start();

            Console.Write("Command> ");

            while (true)
            {
                string input = Console.ReadLine().Trim();

                if (input.ToLower() == "exit")
                {
                    break;
                }
                else
                {
                    if (input.Length > 0)
                    {
                        // use the text passed to the method (by the user)
                        // to creat a new message(Activity)
                        Activity userMessage = new Activity
                        {
                            From = new ChannelAccount(fromUser),
                            Text = input,
                            Type = ActivityTypes.Message
                        };
                        // Post the message to the bot (User messages are sent to the bot using PostActivityAsync method)
                        await client.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
                        Console.WriteLine("PostActivityAsync :  "+ userMessage.From.Id + " Post the message to the bot");  
                        Console.WriteLine("--------------------------------------------------------------------");
                    }
                }
            }
        }
        /// <summary>
        /// call ReadBotMessagesAsync method to read the response from the BOT
        /// </summary>
        /// <param name="client"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        private static async Task ReadBotMessagesAsync(DirectLineClient client, string conversationId)
        {
            // the watermark marks the last message we received
            string watermark = null;

            while (true)
            {
                // Get any messages related to the conversion since the last watermark
                var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                // watermark refresh +1? ( blog said? Set the watermark to the message recerived) 
                watermark = activitySet?.Watermark;

                //Console.WriteLine("-------------------------watermark refreshed ---------------------- :" +watermark);
                // get all the messages(activities)
                var activities = from x in activitySet.Activities
                                 where x.From.Id == botId
                                 select x;
                // loop through each message
                foreach (Activity activity in activities)
                {
                    Console.WriteLine(activity.Text);
                    Console.WriteLine("--------------------------------------------------------------------");
                    Console.WriteLine("-------------------------watermark refreshed ---------------------- :" + watermark);
                    Console.Write("Command> ");
                }

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }
    }
}
