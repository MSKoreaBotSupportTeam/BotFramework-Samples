// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotBuilderSamples;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// 웹 어플리케이션 초기화를 위한 builder 선언
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
});

// 봇 아답터를 통해 인증을 위한 필수 코드, Azure Bot 생성 여부와 상관없이 필요한 코드
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// 전역 에러 핸들러 생성
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// 대화 상태관리를 위한 세션 저장소를 메모리 스토리지로 사용, 운영환경에서는 Redis for Azure 권장
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// 사용자 상태는 싱글톤 타입으로 설정
builder.Services.AddSingleton<UserState>();

// Bot은 Transient 로 지정
builder.Services.AddTransient<IBot, WelcomeUserBot>();

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
