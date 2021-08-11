using Engine.ACME;
using Engine.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

namespace FirstPancake
{
    public static class Function1
    {
        public static Env Prod { get; private set; }
        public static Env Staging { get; private set; }

        [FunctionName("Order")]
        public static async Task<IActionResult> Order(
            [HttpTrigger(AuthorizationLevel.Function,"get", "post", Route = "order")] HttpRequest req)
        {
            AcmeClient service = new AcmeClient();
            await service.CreateAccount("eivydasbal@gmail.com");
            await service.RequestDnsChallengeCertificate("eivydas.in", "abcd1234");
            return new OkObjectResult("OK"); // parasyt kad ok
        }
    }
}
