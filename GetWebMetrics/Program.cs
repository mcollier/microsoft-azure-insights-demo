using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.Azure.Insights;
using Microsoft.Azure.Insights.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace GetWebMetrics
{
    class Program
    {
        private static string _subscriptionId;
        private static string _tenantId;
        private static string _applicationId;
        private static string _applicationPwd;

        private static string _resourceGroupName;
        private static string _siteName;

        static void Main(string[] args)
        {
            _subscriptionId = ConfigurationManager.AppSettings["AzureSubscriptionId"];
            _tenantId = ConfigurationManager.AppSettings["AzureADTenantId"];
            _applicationId = ConfigurationManager.AppSettings["AzureADApplicationId"];
            _applicationPwd = ConfigurationManager.AppSettings["AzureADApplicationPassword"];
            _resourceGroupName = ConfigurationManager.AppSettings["AzureResourceGroupName"];
            _siteName = ConfigurationManager.AppSettings["AzureWebAppName"];

            var token = GetAccessToken();
            var creds = new TokenCloudCredentials(_subscriptionId, token);

            Console.WriteLine("--------- List all metric definitions ---------");           

            var definitions = GetWebMetricDefinitions(creds);

            Task.WaitAll(definitions);

            var response = definitions.Result;
            foreach (var d in response.MetricDefinitionCollection.Value)
            {
                Console.WriteLine("Metric: {0}", d.Name.Value);

                Console.WriteLine("    Time Grains");
                foreach (var x in d.MetricAvailabilities)
                {
                    Console.WriteLine("        {0}", x.TimeGrain);
                }
            }

            Console.WriteLine("Press any key to continue . . . ");
            Console.ReadLine();

            Console.WriteLine("--------- List all metrics ---------");

            var metric = GetWebMetrics(creds);
            Task.WaitAll(metric);

            var metricList = metric.Result;

            foreach (Metric m in metricList.MetricCollection.Value)
            {
                Console.WriteLine("Metric: {0}", m.Name.Value);
                foreach (MetricValue metricValue in m.MetricValues)
                {
                    Console.WriteLine("{0} - {1}", metricValue.Timestamp, metricValue.Average);
                }

                Console.WriteLine();
            }

            Console.WriteLine("Press any key to exit!");
            Console.ReadLine();

        }

        private static string GetAccessToken()
        {
            var authenticationContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", _tenantId));
            var credential = new ClientCredential(clientId: _applicationId, clientSecret: _applicationPwd);
            var result = authenticationContext.AcquireToken(resource: "https://management.core.windows.net/", clientCredential: credential);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            string token = result.AccessToken;

            return token;
        }

        private static async Task<MetricListResponse> GetWebMetrics(TokenCloudCredentials credentials)
        {
            string start = DateTime.UtcNow.AddHours(-1).ToString("yyy-MM-ddTHH:mmZ");
            string end = DateTime.UtcNow.ToString("yyy-MM-ddTHH:mmZ");

            string resourceUri = string.Format("/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}/",
                    _subscriptionId, _resourceGroupName, _siteName);

            //string filterString = string.Format("startTime eq {0} and endTime eq {1} and timeGrain eq duration'PT1M'",
            //        start, end);

            string filterString = string.Format("(name.value eq 'Requests' or name.value eq 'AverageResponseTime') and startTime eq {0} and endTime eq {1} and timeGrain eq duration'PT1M'",
                    start, end);

            CancellationToken ct = new CancellationToken();

            using (var client = new InsightsClient(credentials))
            {
                var x = await client.MetricDefinitionOperations.GetMetricDefinitionsAsync(resourceUri, null);

                return await client.MetricOperations.GetMetricsAsync(resourceUri, filterString,
                    ct);
            }
        }

        private static async Task<MetricDefinitionListResponse> GetWebMetricDefinitions(TokenCloudCredentials credentials)
        {
            string resourceUri =
                string.Format("/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}/",
                    _subscriptionId, _resourceGroupName, _siteName);

            CancellationToken ct = new CancellationToken();

            using (var client = new InsightsClient(credentials))
            {
                return await client.MetricDefinitionOperations.GetMetricDefinitionsAsync(resourceUri, null, ct);
            }
        }
    }
}
