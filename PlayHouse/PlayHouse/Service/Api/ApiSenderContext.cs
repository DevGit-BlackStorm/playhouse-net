// using PlayHouse.Production;
// using PlayHouse.Service.Play;
//
// namespace PlayHouse.Service.Api;
//
// public class ApiSenderContext
// {
//     // AsyncLocal로 ActorSender 객체 저장
//     private static readonly AsyncLocal<IApiSender?> _apiSenderContext = new();
//
//     // private 생성자
//     private ApiSenderContext() { }
//
//     // 싱글톤 인스턴스에 대한 public 접근자
//     //public static ActorSenderContext Instance => _instance;
//     public static IApiSender? Get()
//     {
//         return _apiSenderContext.Value;
//     }
//
//     public static void Set(IApiSender apiSender)
//     {
//         _apiSenderContext.Value = apiSender;
//     }
//         
//     
// }