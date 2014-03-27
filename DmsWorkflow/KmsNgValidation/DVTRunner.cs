using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using KmsNgService.Controllers;

namespace KmsNgWorkflow
{
    public class DVTRunner
    {
        public DVTRunner(string kmsngUrl, string callerName)
        {
            this._kmsngUrl = kmsngUrl;
            this._callerName = callerName;
        }

        public void Run()
        {
            this._proxy = new RestServiceProxy(_kmsngUrl, _callerName);
            try
            {
                ImportKey();
                WaitForReplication(RetryAttempts);
                Decrypt();
                Sign();
            }
            finally
            {
                CleanState();
            }
        }

        private void ImportKey()
        {
            Tracing.Instance.LogMessage(TraceLevel.Info, "ImportKey for {0}", Configuration.AuthorizingTenantName);

            JsonKeyModel keyToImport = new JsonKeyModel()
            {
                jwk = new JWKStructure
                {
                    kid = "new",
                    kty = "RSA",
                    hsmBlob = System.Convert.ToBase64String(Configuration.KeyBlob)
                },
                attributes = new KeyAttributes()
            };

            string importkeyUrl = Configuration.KeyFolder + "?apiversion=" + APIVERSION + "&operation=importkey";
            Tracing.Instance.LogMessage(TraceLevel.Info, "ImportKey to {0}", importkeyUrl);

            HttpResponseMessage response = _proxy.Post(importkeyUrl, keyToImport);
            response.EnsureSuccessStatusCode();
            JsonKeyModel key = Helper.ParseKey(response);
            
            // record key ID and public key
            this._keyId = key.jwk.kid;
            Tracing.Instance.LogMessage(TraceLevel.Info, "KeyID for imported key is {0}", this._keyId);
            this._pubKeyValue = Helper.BuildRsaKey(key);

            Tracing.Instance.LogMessage(TraceLevel.Info, "ImportKey succeed");
        }

        private void Decrypt()
        {
            Tracing.Instance.LogMessage(TraceLevel.Info, "Decrypt message using key {0}", this._keyId);

            string url = GetKeyOperationUrl("decrypt");
            Tracing.Instance.LogMessage(TraceLevel.Info, "Decrypt with {0}", url);
 
            string secretMessage = "To be or not be, that is the question";
            byte[] secretMessageBytes = Encoding.Unicode.GetBytes(secretMessage);
            byte[] secretMessageEncryptedBytes = _pubKeyValue.Encrypt(secretMessageBytes, true);
            
            DecryptRequestBody requestBody = new DecryptRequestBody()
            {
                CipherText = Convert.ToBase64String(secretMessageEncryptedBytes)
            };

            HttpResponseMessage response = this._proxy.Post(url, requestBody);
            response.EnsureSuccessStatusCode();

            // Verify secret successfully decrypted
            byte[] decryptedBytes = Helper.GetDecryptedValue(response);
            string decryptedMessage = Encoding.Unicode.GetString(decryptedBytes);

            bool messageEquals = decryptedMessage.Equals(secretMessage);
            
            Tracing.Instance.LogMessage(
                messageEquals ? TraceLevel.Info : TraceLevel.Error,
                "Decryption message {0} secret message", messageEquals ? "match" : "not match");

            if (!messageEquals)
            {
                Tracing.Instance.LogMessage(TraceLevel.Info, "secrete message {0}. Decrypt message: {1}",
                    secretMessage, decryptedMessage);

                Helper.FailValidation("Decryption failed for {0}", url);
            }

            Tracing.Instance.LogMessage(TraceLevel.Info, "Decrypt succeeded");
        }

        private void Sign()
        {
            Tracing.Instance.LogMessage(TraceLevel.Info, "Sign message using key {0}", this._keyId);
            string url = GetKeyOperationUrl("sign");
            Tracing.Instance.LogMessage(TraceLevel.Info, "Sign with {0}", url);

            string secretMessage = "Whether 'tis Nobler in the mind to suffer";
            byte[] dataToSign = Encoding.Unicode.GetBytes(secretMessage);
            byte[] dataDigestValue = (new SHA256CryptoServiceProvider()).ComputeHash(dataToSign);

            SignRequestBody requestBody = new SignRequestBody
            {
                Digest = Convert.ToBase64String(dataDigestValue)
            };
            
            HttpResponseMessage response = this._proxy.Post(url, requestBody);
            response.EnsureSuccessStatusCode();

            // Validate signature
            SignResponseBody signResponseBody = Helper.ParseSignResponse(response);
            byte[] signatureValue = Convert.FromBase64String(signResponseBody.Signature);
            bool signatureVerified = _pubKeyValue.VerifyHash(dataDigestValue, CryptoConfig.MapNameToOID("SHA256"), signatureValue);

            Tracing.Instance.LogMessage(
                signatureVerified ? TraceLevel.Info : TraceLevel.Error,
                "Signature is {0}", signatureVerified ? "correct" : "not correct");

            if (!signatureVerified)
            {
                Helper.FailValidation("Sign failed for {0}", url);
            }

            Tracing.Instance.LogMessage(TraceLevel.Info, "Sign succeeded");
        }
      
        private void DeleteKey()
        {
            if (this._keyId != null)
            {
                string url = GetKeyOperationUrl("delete");
                Tracing.Instance.LogMessage(TraceLevel.Info, "Delete {0}", url);

                HttpResponseMessage response = this._proxy.Post(url, new object());
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
                {
                    Tracing.Instance.LogMessage(TraceLevel.Error, "Delete key failed for {0}", url);
                }

                this._keyId = null;
            }

            Tracing.Instance.LogMessage(TraceLevel.Info, "Delete key succeeded");
        }

        private bool KeyExists()
        {
            string url = GetKeyUrl();
            HttpResponseMessage response = this._proxy.Get(url);
            return response.IsSuccessStatusCode;
        }

        private void WaitForReplication(int attempts)
        {
            Tracing.Instance.LogMessage(TraceLevel.Info, "Wait MSODS persisting key");

            int i = 0;
            bool keyExist = false;
            while (!keyExist && i++ < attempts)
            {
                Tracing.Instance.LogMessage(TraceLevel.Info, 
                    "The {0} attempt. Sleeping for 10 seconds to give BEC a chance to catch up", i);
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(WaitTimeInSeconds));
                keyExist = KeyExists();
            }

            if (!keyExist)
            {
                this._keyId = null;
                Helper.FailValidation("Imported key not exists after {0} seconds", attempts * WaitTimeInSeconds);
            }

            Tracing.Instance.LogMessage(TraceLevel.Info, "{0} exists in MSODS", GetKeyUrl());
        }

        private void CleanState()
        {
            DeleteKey();

            if (this._proxy != null)
            {
                this._proxy.Dispose();
                this._proxy = null;
            }

            if (this._pubKeyValue != null)
            {
                this._pubKeyValue.Dispose();
                this._pubKeyValue = null;
            }
        }

        private string GetKeyOperationUrl(string operation)
        {
            return GetKeyUrl() + "&operation=" + operation;
        }

        private string GetKeyUrl()
        {
            return Helper.GetKeyPath(this._keyId) + "?apiversion=" + APIVERSION;
        }

        private string _kmsngUrl;
        private string _callerName;

        private RestServiceProxy _proxy;
        private string _keyId;
        private RSACryptoServiceProvider _pubKeyValue;
        
        private const string APIVERSION = "1";
        private const int WaitTimeInSeconds = 10;
        private const int RetryAttempts = 3;
    }
}
