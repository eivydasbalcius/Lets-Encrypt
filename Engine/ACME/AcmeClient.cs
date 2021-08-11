using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.ACME
{
    public class AcmeClient
    {
        static readonly string missingCert = @"C:\Users\EivydasBalcius\Downloads\Doctored Durian Root CA X3.pem";
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
                CommonName = "eivydas.in",
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
    }
}
