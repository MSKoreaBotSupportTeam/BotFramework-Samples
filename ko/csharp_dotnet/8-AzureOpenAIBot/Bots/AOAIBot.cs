// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.18.1

using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// https://github.com/Azure-Samples/azure-search-openai-demo
namespace AzureOpenAIBot.Bots
{
    public class AOAIBot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly AzureServices _azureServices;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;

        public AOAIBot(IConfiguration configuration, AzureServices azureServices, ConversationState conversationState, UserState userState)
        {
            _configuration = configuration;
            _azureServices = azureServices;
            _conversationState = conversationState;
            _userState = userState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationStateAccessor = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessor.GetAsync(turnContext, () => new ConversationData());

            // 유저 질문 저장
            conversationData.UserPrompt.Add(turnContext.Activity.Text);

            var chat_history = GetChatHistoryAsText(conversationData.UserPrompt, conversationData.BotPrompt, includeLastTrun: false);
           

            //chat_history = {0}, question = {1}
            var query_prompt_template = """
                    Below is a history of the conversation so far, and a new question asked by the user that needs to be answered by searching in a knowledge base about employee healthcare plans and the employee handbook.
                    Generate a search query based on the conversation and the new question.
                    Do not include cited source filenames and document names e.g info.txt or doc.pdf in the search query terms.
                    Do not include any text inside[] or <<>> in the search query terms.
                    If the question is not in English, translate the question to English before generating the search query.

                    Chat History:
                    {0}
                    
                    Question:
                    {1}

                    Search query:
                    """;

            var user_prompt = string.Format(query_prompt_template, chat_history, turnContext.Activity.Text);

            //STEP 1:채팅 기록 및 마지막 질문을 기반으로 최적화된 키워드 검색어 생성
            var completion = await _azureServices.OpenAI.GetCompletionsAsync(
                deploymentOrModelName: _configuration["AOAIGPTDeploymentId"], 
                completionsOptions: new CompletionsOptions
                {
                    Prompts = { user_prompt },
                    Temperature = 0.0f, // 0에 가까울수록 있는 그대로 보여주고, 1이 디폴트값,  2에 가까울수록 다양한 문장 생성
                    MaxTokens = 32,
                    StopSequences = { @"\r\n" , @"\n" }
                    
                    // Azure.AI.OpenAI 1.0.0-beta.5 에는 세부 옵션이 미 구현 상태이다.                    
                });

            var searchQuery = completion.Value.Choices.FirstOrDefault().Text;
            searchQuery = searchQuery.Replace("\"", "");


            // STEP 2: GPT 최적화 쿼리로 Search 인덱스에 관련 문서 검색
            var searchResult = await _azureServices.SearchClient.SearchAsync<SearchDocument>(
                searchText: searchQuery,
                options: new SearchOptions()
                {
                    Filter = null,
                    QueryType = SearchQueryType.Semantic,
                    QueryLanguage = QueryLanguage.EnUs,
                    QuerySpeller = QuerySpellerType.Lexicon,
                    SemanticConfigurationName = "default",
                    QueryCaption = null,
                    Size = 3
                    
                });

            string searchResultConcated = "";
            await foreach( var resultItem in searchResult.Value.GetResultsAsync())
            {
                // 결과값의 줄바꿈을 제거하고 소스페이지와 함게 병합한다.
                searchResultConcated += resultItem.Document["sourcepage"] + ": " + resultItem.Document["content"].ToString().Replace("\r\n","").Replace("\n","");
            }


            //var follow_up_questions_prompt_content = """
            //Generate three very brief follow-up questions that the user would likely ask next about their healthcare plan and employee handbook. 
            //Use double angle brackets to reference the questions, e.g. <<Are there exclusions for prescriptions?>>.
            //Try not to repeat questions that have already been asked.
            //Only generate questions and do not generate any text before or after the questions, such as 'Next Questions' 
            //""";


            // follow_up_questions_prompt = {0}, injected_prompt = {1}, sources={2}, chat_history={3}
            var prompt_prefix = """
            <|im_start|>system
            Assistant helps the company employees with their healthcare plan questions, and questions about the employee handbook.Be brief in your answers.
            Answer ONLY with the facts listed in the list of sources below.If there isn't enough information below, say you don't know. Do not generate answers that don't use the sources below. If asking a clarifying question to the user would help, ask the question.
            For tabular information return it as an html table. Do not return markdown format.
            Each source has a name followed by colon and the actual information, always include the source name for each fact you use in the response.Use square brakets to reference the source, e.g. [info1.txt].Don't combine sources, list each source separately, e.g. [info1.txt][info2.pdf].
            {0}
            {1}
            Sources:
            {2}
            <|im_end|>
            {3}
            """;
            user_prompt = string.Format(prompt_prefix, "", "",  searchResultConcated, chat_history);

            // STEP 3: 검색 결과 및 채팅 기록을 사용하여 상황에 맞는 콘텐츠별 답변 생성
            completion = await _azureServices.OpenAI.GetCompletionsAsync(
                deploymentOrModelName: _configuration["AOAIDeploymentId"],
                completionsOptions: new CompletionsOptions
                {
                    Prompts = { user_prompt },
                    Temperature = 0.7f, // 0에 가까울수록 있는 그대로 보여주고, 1이 디폴트값,  2에 가까울수록 다양한 문장 생성
                    MaxTokens = 1024,
                    StopSequences = { "<|im_end|>", "<|im_start|>" }

                    // Azure.AI.OpenAI 1.0.0-beta.5 에는 세부 옵션이 미 구현 상태이다.                    
                });

            searchQuery = completion.Value.Choices.FirstOrDefault().Text;
            searchQuery = searchQuery.Replace("\"", "");

            //결과 값
            var replyText = searchQuery;

            // 봇 응답 저장
            conversationData.BotPrompt.Add(replyText);
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

            // 추가 내용은 별도의 카드 형태로 표시해도 좋다.
            //await turnContext.SendActivityAsync(MessageFactory.Text(searchResultConcated, searchResultConcated), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }

        private string GetChatHistoryAsText(List<string> userPrompts, List<string> botPrompts, bool includeLastTrun = true, int maxToken = 1000)
        {
            string result = "";

            if(includeLastTrun)
            {
                result = "<|im_start|>user\n"  + userPrompts.LastOrDefault() + "\n" + "<|im_end|>\n" +
                    "<|im_start|>assistant\n" + botPrompts.LastOrDefault() + "\n" + "<|im_end|>\n";
            }
            else
            {
                for (int i = userPrompts.Count - 1; i >= 0; i--)
                {
                    // botPrompts 는 항상 userPrompts 보다 1이 작다. 
                    result = "<|im_start|>user\n" + userPrompts[i] + "\n" + "<|im_end|>\n" +
                    "<|im_start|>assistant\n" + (i == botPrompts.Count ? "":  botPrompts[i]  + "\n<|im_end|>\n") + result;
                    if(result.Length > maxToken * 4)
                    {
                        break;
                    }
                }
            }
            return result;

        }
    }
}
