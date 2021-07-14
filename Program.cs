using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace QuotePoster
{
    class Program
    {
        static Random rand = new Random();
        static JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        private static Settings settings;

        static void Main(string[] args)
        {
            LoadSettings();
            var messages = ReadMessages();
            var messageToPost = SelectMessageToPost(messages);
            PostMessage(messageToPost);
            UpdateMessages(messages);
        }

        private static void PostMessage(Message message)
        {
            var client = new RestClient("http://google.com");
            var request = new RestRequest(settings.SlackIncomingWebhook, Method.POST);
            request.AddJsonBody(new
            {
                text = $">{message.Quote}\r\n - {message.Author}"
            });
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                System.Console.WriteLine("There was an error sending the message");
            }
            else
            {
                message.LastPostDateTime = DateTimeOffset.Now;
                System.Console.WriteLine("Message posted at " + message.LastPostDateTime);
                System.Console.WriteLine(message.Quote);
                System.Console.WriteLine($" - {message.Author}");
            }
        }

        private static Message SelectMessageToPost(List<Message> messages)
        {
            System.Console.WriteLine($"{messages.Count} total messages considered");
            var sortedMessages = messages.OrderBy(x => x.LastPostDateTime).ToList();
            var possibleMessages = sortedMessages.Take(Math.Min(sortedMessages.Count(), 10)).ToList();
            var index = rand.Next(possibleMessages.Count);
            System.Console.WriteLine($"Selected message index: {index}");
            return possibleMessages[index];
        }

        private static List<Message> ReadMessages()
            => JsonSerializer.Deserialize<List<Message>>(File.ReadAllText("messages.json"), options);

        private static void UpdateMessages(List<Message> messages)
            => File.WriteAllText("messages.json", JsonSerializer.Serialize(messages, options));

        private static void LoadSettings()
        {
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            var settingsFiles = Directory.EnumerateFiles(Environment.CurrentDirectory, "appsettings-*.json");
            foreach (var settingsFile in settingsFiles)
            {
                builder.AddJsonFile(settingsFile);
            }
            var config = builder.Build();
            settings = new Settings
            {
                SlackIncomingWebhook = config.GetValue<string>(nameof(Settings.SlackIncomingWebhook))
            };
        }            
    }

    public record Message
    {
        public string Quote { get; set; }
        public string Author { get; set; }
        public DateTimeOffset LastPostDateTime { get; set; }
    }

    public record Settings
    {
        public string SlackIncomingWebhook { get; init; }
    }
}
