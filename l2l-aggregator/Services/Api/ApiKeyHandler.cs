using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Api
{
    public class ApiKeyHandler : DelegatingHandler
    {
        private const string ApiKey = "e2fbe0f4fbe2e0fbf4ecf7f1ece5e8f020fbe2e0eff0eae5f020edeeede320fceee8ec343533343536333435212121de2cc1de";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("MTDApikey", ApiKey);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
