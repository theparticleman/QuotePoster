using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace QuotePoster
{
    class Program
    {
        private static Settings settings;
        private static RestClient client = new RestClient("http://google.com");

        static void Main(string[] args)
        {
            LoadSettings();
            var quote = GetQuote();
            PostMessage(quote);
        }

        private static Quote GetQuote()
        {
            var request = new RestRequest("https://api.megamanquotes.com/random-quote", Method.GET);
            var response = client.Execute<Quote>(request);
            Console.WriteLine($"Got a response code of {response.StatusCode} from the API");
            return response.Data;
        }

        private static void PostMessage(Quote quote)
        {
            var request = new RestRequest(settings.SlackIncomingWebhook, Method.POST);
            var source = string.IsNullOrEmpty(quote.Source) ? "" : $" ({quote.Source})";
            request.AddJsonBody(new
            {
                text = $">{quote.Text}\r\n - {quote.Author}{source}"
            });
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                System.Console.WriteLine("There was an error sending the message");
            }
            else
            {
                System.Console.WriteLine("Message posted at " + DateTime.UtcNow);
                System.Console.WriteLine(quote.Text);
                System.Console.WriteLine($" - {quote.Author}{source}");
            }
        }

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

    public record Quote
    {
        public string Text { get; set; }
        public string Author { get; set; }
        public string Source { get; set; }
    }

    public record Settings
    {
        public string SlackIncomingWebhook { get; init; }
    }
}
