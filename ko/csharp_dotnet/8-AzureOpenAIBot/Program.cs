using AzureOpenAIBot;
using AzureOpenAIBot.Bots;
using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;



// 웹 어플리케이션 초기화를 위한 builder 선언
var builder = WebApplication.CreateBuilder(args);

// 로깅 추가
builder.Services.AddLogging();

// 설정 추가
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

//HttpClient 서비스와 Controller 를 추가하는 옵션에서 Json Serialization 속성을 함께 선언
builder.Services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
});

// 봇 아답터를 통해 인증을 위한 필수 코드, Azure Bot 생성 여부와 상관없이 필요한 코드
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// 전역 에러 핸들러 생성
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddSingleton<ConversationState>();

//AOAI 와 Azure Search 연결
builder.Services.AddSingleton<AzureServices>();

// EchoBot을 세션마다 생성하도록 종속성 주입. 
// 참고 : https://learn.microsoft.com/ko-kr/dotnet/core/extensions/dependency-injection#transient
builder.Services.AddTransient<IBot, AOAIBot>();



//설정을 빌드 한 app 구성
var app = builder.Build();

// Visual Studio 에서 실행할 때는 /Properties/launchSettings.json 의 선언에 따라 기본으로는 ASPNETCORE_ENVIRONMENT="Development" 로 실행되므로 개발환경으로 인식.
// Azure App Service 에 배포 하는 경우 본 설정이 없기 때문에 조건에서 False 가 되고,
// VM, App Service 또는 Container 인 경우 ASPNETCORE_ENVIRONMENT 시스템 변수에 "Development", "Staging", "Production" 값 설정에 따라 처리됨
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseDefaultFiles()
    .UseStaticFiles()
    //.UseWebSockets() //웹소켓을 사용하는 경우에만 활성화
    .UseRouting()
    //.UseAuthorization() // 본 예제에서는 인증을 사용하지 않으므로 필요X, AppID 와 AppPassword 를 사용하는 경우 반드시 사용 필요.
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });

app.Run();
