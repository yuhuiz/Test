using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace KmsNgWorkflow
{
    public sealed class RestServiceProxy : IDisposable
    {
        public RestServiceProxy(string kmsngUrl, string callerName)
        {
            this._callerName = callerName;
            this._client = new HttpClient();
            this._client.DefaultRequestHeaders.Clear();
            this._client.BaseAddress = new Uri(kmsngUrl);
            this._client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            InitializeAuthorizationBearerToken();
        }

        private void InitializeAuthorizationBearerToken()
        {
            try
            {
                ClientCredential clientCredential;
                AuthenticationContext authenticationContext;
                AuthenticationResult authenticationResult;

                // Create client credential
                clientCredential = new ClientCredential(Configuration.ClientId, Configuration.ClientSecret);

                // Create authorization context
                authenticationContext = new AuthenticationContext(Configuration.AuthorizingTenant, null);

                // Retrieve an access token from ESTS 
                Tracing.Instance.LogMessage(TraceLevel.Info, "{0} - Calling ESTS - ClientId {1}, ResourceAppId {2}, AuthorizingTenant {3}",
                    this._callerName, Configuration.ClientId, Configuration.ResourceAppId, Configuration.AuthorizingTenant);

                authenticationResult = authenticationContext.AcquireToken(Configuration.ResourceAppId, clientCredential);

                // Add the access token as a bearer token in the Authorization header
                this._client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
            }
            catch (ActiveDirectoryAuthenticationException ex)
            {
                Tracing.Instance.LogMessage(TraceLevel.Error, "ESTS authentication threw an exception.  {0}", ex.ToString());
                throw;
            }
        }

        public HttpResponseMessage Get(string requestUri)
        {
            Tracing.Instance.LogMessage(TraceLevel.Info, "{0} - Calling KMSNG - GET {1}", this._callerName, this._client.BaseAddress + requestUri);
            return _client.GetAsync(requestUri).Result;
        }

        public HttpResponseMessage Post<T>(string requestUri, T value)
        {
            Tracing.Instance.LogMessage(TraceLevel.Info, "{0} - Calling KMSNG - POST {1}, {2}", this._callerName, _client.BaseAddress + requestUri, value.GetType().Name);
            return _client.PostAsJsonAsync<T>(requestUri, value).Result;
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
        }

        private HttpClient _client;
        private string _callerName;
    }
}
