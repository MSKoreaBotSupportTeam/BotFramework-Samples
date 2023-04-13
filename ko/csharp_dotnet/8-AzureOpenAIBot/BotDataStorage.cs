//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System;

//namespace AzureOpenAIBot
//{
//    public static class BotDataStorage<T>
//    {
//        internal static int MaxRetryies = 3;

//        /// <summary>
//        /// 값 저장
//        /// </summary>
//        /// <param name="context"></param>
//        /// <param name="key"></param>
//        /// <param name="value"></param>
//        public static void SetProperty(DataType type, string key, T value)
//        {
//            int retries = 0;

//            while (retries <= MaxRetryies)
//            {
//                try
//                {
//                    MemoryCache.SetValue<string>(type + "-" + key, JsonConvert.SerializeObject(value));
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.Log(ex);

//                    retries++;
//                }
//            }
//        }

//        /// <summary>
//        /// 값 반환
//        /// </summary>
//        /// <param name="type"></param>
//        /// <param name="key"></param>
//        /// <returns></returns>
//        public static T GetProperty(DataType type, string key)
//        {
//            T result = default(T);

//            int retries = 0;

//            while (retries <= MaxRetryies)
//            {
//                try
//                {
//                    var data = MemoryCache.GetValue<string>(type + "-" + key) ?? null;

//                    if (data != null)
//                        result = JsonConvert.DeserializeObject<T>(data);
//                    break;
//                }
//                catch (Exception ex)
//                {

//                    _logger.Log(ex);
//                    retries++;
//                }
//            }

//            return result;
//        }

//        /// <summary>
//        /// 값 삭제
//        /// </summary>
//        /// <param name="context"></param>
//        /// <param name="key"></param>
//        public static async Task RemoveProperty(DataType type, string key)
//        {
//            try
//            {
//                MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
                
//                await Task.Run(() => MemoryCache.Remove<string>(type + "-" + key));
//            }
//            catch (Exception ex)
//            {
//                _logger.Log(ex);
//            }
//        }

//        /// <summary>
//        /// 이전 Dialog 정보 삭제
//        /// </summary>
//        /// <param name="context"></param>
//        /// <returns></returns>
//        public static async Task RemoveBeforeDialog(string conversationId)
//        {
//            await BotDataStorage<Stack<string>>.RemoveProperty(DataType.DialogHistory, conversationId);
//            await BotDataStorage<Stack<string>>.RemoveProperty(DataType.ConversationHistory, conversationId);
//        }
//    }

//    /// <summary>
//    /// 지정된 타입만 저장하도록
//    /// </summary>
//    public enum DataType
//    {
//        ChatHistory,
//        ConversationHistory,
//        DialogHistory,
//        LuisResult,
//        QnAId,
//        /// <summary>
//        /// 사용자 설정값
//        /// </summary>
//        UserData,
//        /// <summary>
//        /// 카카오톡 유저키
//        /// </summary>
//        KakaoUserKey,
//        /// <summary>
//        /// FAQ의 재질문 횟수
//        /// </summary>
//        RetryQuestionCount,
//        /// <summary>
//        /// FAQ의 더보기 횟수
//        /// </summary>
//        RetryQuestionMoreCount,
//        /// <summary>
//        /// 관리자 여부
//        /// </summary>
//        SuperUser,
//        /// <summary>
//        /// social data
//        /// </summary>
//        SocialData,
//        /// <summary>
//        /// 언어
//        /// </summary>
//        Lang,
//        /// <summary>
//        /// 메인메뉴 선택값
//        /// </summary>
//        MainMenu,
//        LUISCall
//    }
//}
