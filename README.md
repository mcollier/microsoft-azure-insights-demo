# microsoft-azure-insights-demo

*This is a sample application only. Use at your own risk.*

This sample demonstrates how to use the Microsoft Azure Insights API to programatically retrieve metric data.


Retrieving the metric data is also possible via the [Insights REST API](https://msdn.microsoft.com/en-us/library/azure/dn931943.aspx)

**Get Metric Values**
GET https://management.azure.com/subscriptions/{subscription_id}/resourceGroups/{resource_group}/providers/Microsoft.Web/sites/{resource}/metrics?api-version=2014-04-01&$filter={filter}

**[Get Metric Definitions](https://msdn.microsoft.com/en-us/library/azure/dn931939.aspx)**
GET https://management.azure.com/subscriptions/{subscription_id}/resourceGroups/{resource_group}/providers/Microsoft.Web/sites/{resource}/metricDefinitions?api-version=2014-04-01




Thanks to the post at https://yossidahan.wordpress.com/2015/02/13/reading-metric-data-from-azure-using-the-azure-insights-library/ for the inspiration.