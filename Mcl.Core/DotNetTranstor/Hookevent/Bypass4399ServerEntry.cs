using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetTranstor;
using DotNetTranstor.Hookevent;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.DotNetTranstor.Hookevent
{
    /// <summary>
    /// 绕过4399无法进服
    /// 拦截 WPF启动器向 https://x19apigatewayobt.nie.netease.com/item/user-is-purchase-item 发送的POST请求
    /// 自动返回伪造的成功响应，entity_id 替换为请求中的 item_id
    /// </summary>
    public class Bypass4399ServerEntry : IMethodHook
    {
        private const string TARGET_URL_PATH = "/item/user-is-purchase-item";

        [OriginalMethod]
        public Task<HttpResponseMessage> Original_SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return null;
        }

        [HookMethod("System.Net.Http.HttpClient", "SendAsync", "Original_SendAsync")]
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 检查是否启用了绕过功能，并且请求URL匹配目标
            if (Path_Bool.Bypass4399ServerEntry && 
                request != null && 
                request.RequestUri != null &&
                request.Method == HttpMethod.Post &&
                request.RequestUri.AbsolutePath.Contains(TARGET_URL_PATH))
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[Bypass4399ServerEntry] 拦截到进服验证请求: {request.RequestUri}");

                    // 读取请求体内容
                    string requestBody = request.Content?.ReadAsStringAsync().Result ?? "{}";
                    Console.WriteLine($"[Bypass4399ServerEntry] 请求体: {requestBody}");

                    // 解析JSON提取item_id
                    JObject requestJson = JObject.Parse(requestBody);
                    string itemId = requestJson["item_id"]?.ToString() ?? "xxx";

                    Console.WriteLine($"[Bypass4399ServerEntry] 使用 item_id 作为 entity_id: {itemId}");

                    // 构造伪造的成功响应
                    string fakeResponse = $"{{\"code\":0,\"details\":\"\",\"entity\":{{\"entity_id\":\"{itemId}\"}},\"message\":\"正常返回\"}}";
                    Console.WriteLine($"[Bypass4399ServerEntry] 返回伪造响应: {fakeResponse}");

                    Console.ResetColor();

                    var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                    response.Content = new StringContent(fakeResponse, Encoding.UTF8, "application/json");
                    return Task.FromResult(response);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Bypass4399ServerEntry] 处理请求时出错: {ex.Message}");
                    Console.ResetColor();
                }
            }

            // 未匹配或不满足条件，调用原始方法
            return Original_SendAsync(request, cancellationToken);
        }
    }
}
