using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using KmsNgService.Controllers;

namespace KmsNgWorkflow
{
    public class Helper
    {
        // This is modiefied first time
        public static JsonKeyModel ParseKey(HttpResponseMessage message)
        {
            return message.Content.ReadAsAsync<JsonKeyModel>().Result;
        }
	
	// This is modified second time
        public static DecryptResponseBody ParseDecryptResponse(HttpResponseMessage message)
        {
            return message.Content.ReadAsAsync<DecryptResponseBody>().Result;
        }

        // This is modified third time
        public static RSACryptoServiceProvider BuildRsaKey(JsonKeyModel key)
        {
            var keyParameters = new RSAParameters();
            keyParameters.Modulus = System.Convert.FromBase64String(key.jwk.n);
            keyParameters.Exponent = System.Convert.FromBase64String(key.jwk.e);

            var rsaKey = new RSACryptoServiceProvider();
            rsaKey.ImportParameters(keyParameters);
            return rsaKey;
        }

	// This is first modification in testing branch
	// This is the first modified after switch from testing branch
	// This is the first modification from HOTFix branch
        // First modification after merging conflict with testing branch to test common-ancestor base	
        public static string GetKeyPath(string keyId)
        {
    	    // This is second modification in testing branch
            return (String.Format("{0}/{1}", Configuration.KeyFolder, keyId));
        }
	
        public static byte[] GetDecryptedValue(HttpResponseMessage response)
        {
            var decryptResponse = ParseDecryptResponse(response);
            return Convert.FromBase64String(decryptResponse.PlainText);
        }

        public static void FailValidation(string format, params object[] args)
        {
            Tracing.Instance.LogMessage(TraceLevel.Error, format, args);
            throw new InvalidOperationException(string.Format(format, args));
        }

        public static SignResponseBody ParseSignResponse(HttpResponseMessage message)
        {
            return message.Content.ReadAsAsync<SignResponseBody>().Result;
        }
       
    }

}
