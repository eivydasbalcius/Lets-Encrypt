using Certes.Acme;
using System;
using System.Collections.Generic;

//Dictionary for selecting Acme client server (Staging or Production)
namespace Engine.Utility
{
    public enum Env
    {
        Staging,
        Prod
    }
    public static class Enviroment
    {
        private static Dictionary<Env, Uri> EnviromentDic = new Dictionary<Env, Uri>();
        public static void SetEnviroment()
        {
            EnviromentDic.Clear();
            EnviromentDic.Add(Env.Prod, WellKnownServers.LetsEncryptV2);
            EnviromentDic.Add(Env.Staging, WellKnownServers.LetsEncryptStagingV2);
        }
        public static Uri GetUri(Env env)
        {
            return EnviromentDic.GetValueOrDefault(env);
        }
    }
}
