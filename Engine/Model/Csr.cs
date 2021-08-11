using Newtonsoft.Json;

namespace Engine.Model
{
    //Certificate signing request. Trying to create in JSNO format kad paduoti i Body
    public class Csr
    {
        [JsonProperty(PropertyName = "countryName")]
        public string CountryName { get; private set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; private set; }

        [JsonProperty(PropertyName = "locality")]
        public string Locality { get; private set; }

        [JsonProperty(PropertyName = "organization")]
        public string Organization { get; private set; }

        [JsonProperty(PropertyName = "organizationUnit")]
        public string OrganizationUnit { get; private set; }

        [JsonProperty(PropertyName = "commonName")]
        public string CommonName { get; private set; }

        [JsonConstructor]
        public Csr(string countryName, string state, string locality, string organization, string organizationUnit, string commonName)
        {
            CountryName = countryName;
            State = state;
            Locality = locality;
            Organization = organization;
            OrganizationUnit = organizationUnit;
            CommonName = commonName;
        }


    }
}
