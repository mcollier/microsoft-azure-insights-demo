using System;
using System.Configuration;
using System.Text;
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

        private static string _webAppResourceGroupName;
        private static string _siteName;
        private static string _vmName;
        private static string _vmResourceGroupName;

        private const string WebAppResourceUriFormat = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}/";
        private const string VirtualMachineResourceUriFormat = "/subscriptions/{0}/resourceGroups/{1}/providers/microsoft.classiccompute/virtualmachines/{2}/";

        static void Main(string[] args)
        {
            _subscriptionId = ConfigurationManager.AppSettings["AzureSubscriptionId"];
            _tenantId = ConfigurationManager.AppSettings["AzureADTenantId"];
            _applicationId = ConfigurationManager.AppSettings["AzureADApplicationId"];
            _applicationPwd = ConfigurationManager.AppSettings["AzureADApplicationPassword"];
            _webAppResourceGroupName = ConfigurationManager.AppSettings["AzureWebAppResourceGroupName"];
            _siteName = ConfigurationManager.AppSettings["AzureWebAppName"];
            _vmResourceGroupName = ConfigurationManager.AppSettings["AzureClassicVmResourceGroupName"];
            _vmName = ConfigurationManager.AppSettings["AzureClassicVmName"];


            string webAppResourceUri = string.Format(WebAppResourceUriFormat, _subscriptionId, _webAppResourceGroupName, _siteName);
            string classicVmResourceUri = string.Format(VirtualMachineResourceUriFormat, _subscriptionId, _vmResourceGroupName, _vmName);

            var token = GetAccessToken();
            var creds = new TokenCloudCredentials(_subscriptionId, token);

            /* Web App */
            Console.WriteLine("--------- List available Web App metric definitions ---------");

            MetricDefinitionListResponse webMetricDefinitions = GetAvailableMetricDefinitions(creds, webAppResourceUri);
            PrintMetricDefinitions(webMetricDefinitions);
            
            Console.WriteLine("--------- List Web App metrics ---------");

            MetricListResponse webMetricList = GetResourceMetrics(creds, webAppResourceUri, string.Empty, TimeSpan.FromHours(1), "PT1M" );
            PrintMetricValues(webMetricList);


            /* Classic Virtual Machine */
            Console.WriteLine("--------- List Classic Compute metrics ---------");

            MetricDefinitionListResponse vmMetricDefinitions = GetAvailableMetricDefinitions(creds, classicVmResourceUri);
            PrintMetricDefinitions(vmMetricDefinitions);

            string filter = "(name.value eq 'Percentage CPU' or name.value eq 'Network In')";
            MetricListResponse vmMetricList = GetResourceMetrics(creds, classicVmResourceUri, filter, TimeSpan.FromHours(1), "PT1H");
            PrintMetricValues(vmMetricList);



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

        private static void PrintMetricValues(MetricListResponse metricList)
        {
            foreach (Metric m in metricList.MetricCollection.Value)
            {
                Console.WriteLine("Metric: {0}", m.Name.Value);
                foreach (MetricValue metricValue in m.MetricValues)
                {
                    Console.WriteLine("{0} - {1}", metricValue.Timestamp, metricValue.Average);
                }

                Console.WriteLine();
            }
        }

        private static void PrintMetricDefinitions(MetricDefinitionListResponse definitions)
        {
            foreach (var d in definitions.MetricDefinitionCollection.Value)
            {
                Console.WriteLine("Metric: {0}", d.Name.Value);

                Console.WriteLine("    Time Grains");
                foreach (var x in d.MetricAvailabilities)
                {
                    Console.WriteLine("        {0}", x.TimeGrain);
                }

                Console.WriteLine();
            }
        }

        private static MetricListResponse GetResourceMetrics(TokenCloudCredentials credentials, string resourceUri, string filter, TimeSpan period, string duration)
        {
            var dateTimeFormat = "yyy-MM-ddTHH:mmZ";

            string start = DateTime.UtcNow.Subtract(period).ToString(dateTimeFormat);
            string end = DateTime.UtcNow.ToString(dateTimeFormat);

            // TODO: Make this more robust.
            StringBuilder sb = new StringBuilder(filter);

            if (!string.IsNullOrEmpty(filter))
            {
                sb.Append(" and ");
            }
            sb.AppendFormat("startTime eq {0} and endTime eq {1}", start, end);
            sb.AppendFormat(" and timeGrain eq duration'{0}'", duration);

            using (var client = new InsightsClient(credentials))
            {
                return client.MetricOperations.GetMetrics(resourceUri, sb.ToString());
            }
        }

        private static MetricDefinitionListResponse GetAvailableMetricDefinitions(TokenCloudCredentials credentials, string resourceUri)
        {
            using (var client = new InsightsClient(credentials))
            {
                return client.MetricDefinitionOperations.GetMetricDefinitions(resourceUri, null);
            }
        }
    }
}
