using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using System.Collections.Generic;

namespace Engine.ACME
{
    public class AcmeClient
    {
        // Details for subscription and Service Principal account
        // This demo assumes the Service Principal account uses password-based authentication (certificate-based is also possible)
        // See https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/ for details
        string tenantId = "0a032824-fa0d-4fc8-8ed3-cc4dab89ad50";
        string clientId = "c6bcc25b-8445-43f3-b8ae-c66b1cd03774";
        string secret = "gcpQ.O8a7hd34.v6x6g_JBu4xz6YXQN6..";
        string subscriptionId = "f2d3b1eb-4201-445e-8ca3-94ea5ead6bb2";

        // The resource group which this this sample will use.
        // This needs to exist already, and the Service Principal needs to have been granted 'DNS Zone Contributor' permissions to the resource group
        string resourceGroupName = "rg-acme";

        // The DNS zone name which this sample will use.
        // Does not need to exist, will be created and deleted by this sample.
        string zoneName = "eivydas.in";


        static readonly string missingCert = @"C:\Users\EivydasBalcius\source\repos\FirstPancake\Missing certificate\Doctored Durian Root CA X3.pem";
        public AcmeContext Context { get; private set; }
        public IAccountContext Account { get; private set; }
        public AcmeContext Acme { get; private set; }

        //Creating Acme client context. Setting Acme client server ((STAGING) - WellKnownServers.LetsEncryptStagingV2 / (PRODUCTION) - WellKnownServers.LetsEncryptV2)
        public AcmeClient()
        {
            Context = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
        }

        public async Task CreateAccount(string email)
        {
            //Creating new Let's Encrypt account
            Account = await Context.NewAccount(email, true);
            //Confirming account to Acme client with Account Key
            Acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2, Context.AccountKey);
        }
        public string GetPemKey()
        {
            return Context.AccountKey.ToPem();
        }

        public IKey GetAccountKey()
        {
            return Context.AccountKey;
        }

        public async Task RequestDnsChallengeCertificate(string dns, string PFXPassword)
        {
            //creating new order for specific domain
            var order = await Context.NewOrder(new[] { dns });
            //authorizing order
            //.First() - takes first element from order array
            var auth = (await order.Authorizations()).First();

            //requesting challange
            var dnsChallenge = await auth.Dns();
            //getting dnsTXT record 
            var dnsTxt = Context.AccountKey.DnsTxt(dnsChallenge.Token);
            Console.Out.WriteLine($"Got DNS challenge token {dnsTxt}");
            await CreateDnsTxtRecord(tenantId, clientId, secret, subscriptionId, resourceGroupName, zoneName, dnsTxt);
            //Validating challange. Waiting for dnsTXT token to be imported to DNS record (type: TXT, name:_acme-challange, value: token)
            Challenge chalResp = await dnsChallenge.Validate();

            //checking challange status. When status is "Valid" order can be created.
            while (chalResp.Status == ChallengeStatus.Pending || chalResp.Status == ChallengeStatus.Processing)
            {
                Console.WriteLine($"DNS challange response status {chalResp.Status} more info at {chalResp.Url.ToString()} retrying in 5 sec");
                await Task.Delay(5000);
                chalResp = await dnsChallenge.Resource();
            }
            Console.WriteLine($"Finished validating DNS challange Token, response is {chalResp.Status} more info at {chalResp.Url.ToString()}");

            //Certificate signing request
            var csrInfo = new Certes.CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = dns,
            };

            //Creating private key with algorithm (RSA key)
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);

            //Generating certificate wit created CSR and private key
            var cert = await order.Generate(csrInfo, privateKey);

            //Adding missing certificate (Doctored Durian Root CA X3)
            var text = File.ReadAllText(missingCert);
            byte[] issuer = Encoding.ASCII.GetBytes(text);

            var pfxBuilder = cert.ToPfx(privateKey);
            pfxBuilder.AddIssuer(issuer);
            //Building certificare in PFX format
            var pfx = pfxBuilder.Build(dns, PFXPassword);
            File.WriteAllBytes(@"C:\Users\EivydasBalcius\authKey\certificate\ValidCert.pfx", pfx);


            //Http challange code (not working fine)

            /*          var httpChallenge = await auth.Http();
                        var certChain = await order.Download();
                        var pemCert = certChain.Certificate.ToPem();
                        var keyToPem = privateKey.ToPem();
                        File.WriteAllText(@"C:\Users\EivydasBalcius\authKey\certificate\cert.pem", pemCert);
                        File.WriteAllText(@"C:\Users\EivydasBalcius\authKey\certificate\key.key", keyToPem);
                        var theCert = string.Join(Environment.NewLine, pemCert + keyToPem);
                        File.WriteAllText(@"C:\Users\EivydasBalcius\authKey\certificate\theCert.pem", theCert);*/
        }

        public static async Task CreateDnsTxtRecord(string tenantId, string clientId, string secret, string subscriptionId, string resourceGroupName, string zoneName, string dnsTxt)
        {
            // Build the service credentials and DNS management client
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret);
            var dnsClient = new DnsManagementClient(serviceCreds);
            dnsClient.SubscriptionId = subscriptionId;

            // **********************************************************************************************************
            // Create TXT Record
            // **********************************************************************************************************

            // Note: Service and Protocol are part of the record set name

            var recordSetName = "_acme-challenge";
            Console.Write("Creating DNS 'TXT' record set with name '{0}'...", recordSetName);
            try
            {
                // Create record set parameters
                var recordSetParams = new RecordSet();
                recordSetParams.TTL = 3600;

                // Add record to the record set parameter object. 
                // For each record, create a list of strings, max 255 characters per string (clients concatenate the strings within each record and treat as a single string)
                recordSetParams.TxtRecords = new List<TxtRecord>();
                var strings = new List<string>();
                strings.Add(dnsTxt);
                recordSetParams.TxtRecords.Add(new TxtRecord(strings));

                // Create the actual record set in Azure DNS
                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                var recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.TXT, recordSetParams);

                Console.WriteLine("success");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("failed: {0}", e.Message);
            }
        }
    }
}
