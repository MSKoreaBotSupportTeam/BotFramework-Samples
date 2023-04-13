using Microsoft.Bot.Configuration;
using Microsoft.Extensions.Configuration;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using System;
using Azure;
using Microsoft.Extensions.Logging;

namespace AzureOpenAIBot
{
    public class AzureServices
    {
        public OpenAIClient OpenAI { get; set; }

        public SearchClient SearchClient { get; set; }

        public AzureServices(IConfiguration configuration, ILogger<AzureServices> logger)
        {
            logger.LogInformation("AzureServices 생성자 호출");

            // openai 초기화
            var openaiUri = new Uri($"https://{configuration["AOAIName"]}.openai.azure.com");
            OpenAI = new OpenAIClient(openaiUri, new AzureKeyCredential(configuration["AOAIApiKey"]));

            // Azure Search 초기화
            var searchUri = new Uri($"https://{configuration["SearchServiceName"]}.search.windows.net");
            SearchClient = new SearchClient(searchUri, configuration["SearchIndexName"], new AzureKeyCredential(configuration["SearchApiKey"]));
            
            logger.LogInformation("AzureServices 생성자 종료");
        }
    }
}
