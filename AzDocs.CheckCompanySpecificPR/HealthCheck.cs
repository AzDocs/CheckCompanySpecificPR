using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace AzDocs.CheckCompanySpecificPR
{
    public class HealthCheck
    {
        [FunctionName("HealthCheck")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestMessage req)
        {
            return req.CreateResponse(HttpStatusCode.OK);

        }
    }
}
