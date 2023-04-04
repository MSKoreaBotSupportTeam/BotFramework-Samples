// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

namespace Microsoft.BotBuilderSamples
{
    // Represents a bot that processes incoming activities.
    // For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    // This is a Transient lifetime service. Transient lifetime services are created
    // each time they're requested. For each Activity received, a new instance of this
    // class is created. Objects that are expensive to construct, or have a lifetime
    // beyond the single turn, should be carefully managed.
    // For example, the "MemoryStorage" object and associated
    // IStatePropertyAccessor{T} object are created with a singleton lifetime.
    public class WelcomeUserBot : ActivityHandler
    {
        // Messages sent to the user.
        private const string WelcomeMessage = "웰컴봇 샘플입니다. 사용자가 봇에 접속할때 먼저 인사를 하고는 예제이며, " +
                                            "본 예제에 포함된 명령어는 hi, help 입니다. " +
                                            "hi 를 입력한 경우 echo 로 사용자가 입력한 hi 를 다시 출력해 줍니다." +
                                            "help 를 입력하면 간단한 HeroCard 를 출력합니다." +
                                            "또한 이 메시지는 사전에 정의되지 않은 명령어가 입력되는 경우에도 출력됩니다.";

        private const string InfoMessage = "이 메시지가 보이는 이유는 처음 ConversationUpdate 이벤트가 발생되어" +
                                            "OnMembersAddedAsync 함수에서 보여주는 메시지 입니다.";

        private const string LocaleMessage = "설정된 언어를 'GetLocale()' 함수로 판단하여 환영 인사를 해당 언어로 지정할 수 있습니다." +
                                             "에뮬레이터의 경우 Local이 en-US가 디폴트로 설정되어 있습니다.";

        private const string PatternMessage = "It is a good pattern to use this event to send general greeting" +
                                              "to user, explaining what your bot can do. In this example, the bot " +
                                              "handles 'hello', 'hi', 'help' and 'intro'. Try it now, type 'hi'";

        private readonly BotState _userState;

        // Program.cs 에서 AddSingleton 타입으로 종속성을 주입하였습니다.
        public WelcomeUserBot(UserState userState)
        {
            _userState = userState;
        }

        // 사용자와 봇 사이에 처음으로 ConversationUpdate 이벤트가 발생될때 실행되는 함수.
        // 모든 채널에서 본 함수가 트리거 된다고 보장하지는 못하므로 향후 다른 예제에서 소개될 OnTurnAsync() 함수에서 처리하는 것이 좋습니다.
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    //인증 없이 익명으로 접속하는 경우 member.Name 은 User 가 됩니다.
                    await turnContext.SendActivityAsync($"Hi there - {member.Name}. {WelcomeMessage}", cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync(InfoMessage, cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync($"{LocaleMessage} Current locale is '{turnContext.Activity.GetLocale()}'.", cancellationToken: cancellationToken);
                    //await turnContext.SendActivityAsync(PatternMessage, cancellationToken: cancellationToken);
                }
            }
        }

        // 본 코드는 에뮬레이터에서 이벤트가 발생되지는 않지만, 채널 중 대화종료 기능구현이 필수 인 경우에 사용될 수 있습니다.
        protected override async Task OnMembersRemovedAsync(IList<ChannelAccount> membersRemoved, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach(var member in membersRemoved)
            {
                var temp = member;
            }
            await Task.CompletedTask;
        }

        // 사용자가 메시지를 입력하면 호출되는 함수
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // 사용자 상태를 저장하기 위해 사용자의 상태속성접근자 선언(싱글톤 수명주기 동안)
            // !! 사용자 상태와 대화 상태는 세션처럼 관리되어야 하며 본 예제에서는 메모리에 저장합니다.
            //    스케일 아웃되는 패턴의 경우 메모리에서 관리 하는것 보다 Redis 캐시에서 관리되어야 하고, 상태값을 SetAsync(), GetAsync() 함수를 통해 변경하게 됩니다.  
            //    상태 변경이 있는 경우 반드시 115번줄의 SetAsync()함수로 상태를 업데이트 해야 합니다.
            var welcomeUserStateAccessor = _userState.CreateProperty<WelcomeUserState>(nameof(WelcomeUserState));
            var didBotWelcomeUser = await welcomeUserStateAccessor.GetAsync(turnContext, () => new WelcomeUserState(), cancellationToken);

            if (didBotWelcomeUser.DidBotWelcomeUser == false)
            {
                didBotWelcomeUser.DidBotWelcomeUser = true;

                // the channel should sends the user name in the 'From' object
                var userName = turnContext.Activity.From.Name;

                await turnContext.SendActivityAsync("사용자가 봇에서 처음으로 메시지를 보내는 경우 이 메시지를 출력하게 됩니다. 상황에 따라서 필요한 로직을 여기에 둘 수 있습니다.", cancellationToken: cancellationToken);
                await turnContext.SendActivityAsync($"사용자의 유일한 이름으로 이전에 대화한 이력이 있는 경우 여기서 분기처리를 할 수 있습니다. 사용자 이름: {userName}.", cancellationToken: cancellationToken);
            }
            else
            {
                // This example hardcodes specific utterances. You should use LUIS or QnA for more advance language understanding.
                var text = turnContext.Activity.Text.ToLowerInvariant();
                switch (text)
                {
                    case "hello":
                    case "hi":
                        await turnContext.SendActivityAsync($"You said {text}.", cancellationToken: cancellationToken);
                        break;
                    case "intro":
                    case "help":
                        await SendIntroCardAsync(turnContext, cancellationToken);
                        break;
                    default:
                        await turnContext.SendActivityAsync(WelcomeMessage, cancellationToken: cancellationToken);
                        break;
                }
            }
            // 사용자 상태 저장.
            await _userState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Title = "안녕하세요!",
                Subtitle = "환영 인사 예제 봇 입니다. 본 카드는 HeroCard 이며  버튼을 통해 외부링크를 열거나 텍스트를 입력 할 수 있습니다.",
                Images = new List<CardImage>() { new CardImage("https://microsoft.github.io/botframework-solutions/assets/images/icons/virtual-assistant.png") },
                Buttons = new List<CardAction>()
                {
                    new CardAction(ActionTypes.OpenUrl, "개요", null, "Get an overview", "Get an overview", "https://docs.microsoft.com/ko-kr/azure/bot-service/?view=azure-bot-service-4.0"),                    
                    new CardAction(ActionTypes.OpenUrl, "Azure에 봇 배포", null, "Azure에 봇 배포", "Azure에 봇 배포", "https://learn.microsoft.com/ko-kr/azure/bot-service/provision-and-publish-a-bot?view=azure-bot-service-4.0&tabs=userassigned%2Ccsharp"),
                    new CardAction(ActionTypes.ImBack, "채팅창에 hi 입력", null, "채팅창에 hi 입력", "채팅창에 hi 입력", "hi")
                }
            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }
    }
}
