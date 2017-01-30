using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace GetResourceMetrics
{
    /// <summary>
    /// Create Credentials for Microsoft.Rest
    /// based on: http://stackoverflow.com/questions/35228042/how-to-create-serviceclientcredential-to-be-used-with-microsoft-azure-management 
    /// </summary>
    public class InsightsClientCredentials : ServiceClientCredentials
    {
        private string _tenantId = null;
        private string _clientId = null;
        private string _clientSecret = null;
        private string _resource = "https://management.core.windows.net/";
        private Version _apiVersion = null;

        public InsightsClientCredentials(string clientId, string clientSecret,string subscriptionId, string tenantId, Version apiVersion = null)
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _clientSecret = clientSecret;
            this.SubscriptionId = subscriptionId;
        }

        private string AuthenticationToken { get; set; }
        public string SubscriptionId { get; set; }
        public override void InitializeServiceClient<T>(ServiceClient<T> client)
        {
            var authenticationContext =
                new AuthenticationContext($"https://login.windows.net/{_tenantId}");
            var credential = new ClientCredential(clientId: _clientId, clientSecret: _clientSecret);

            var result = authenticationContext.AcquireTokenAsync(resource: _resource,
                clientCredential: credential).Result;

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }
            AuthenticationToken = result.AccessToken;
        }
        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (AuthenticationToken == null)
                throw new InvalidOperationException("Token Provider Cannot Be Null");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthenticationToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (_apiVersion != null) request.Version = _apiVersion;
            await base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}
