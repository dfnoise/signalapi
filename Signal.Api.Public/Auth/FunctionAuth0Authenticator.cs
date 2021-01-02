using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Signal.Core;
using Signal.Core.Auth;

namespace Signal.Api.Public.Auth
{
    public class FunctionAuth0Authenticator : IFunctionAuthenticator
    {
        private const string RefreshTokenUrlPath = "/oauth/token";
        private readonly ISecretsProvider secretsProvider;
        private readonly ILogger<FunctionAuth0Authenticator> logger;
        private Auth0Authenticator? authenticator;
        
        public FunctionAuth0Authenticator(
            ISecretsProvider secretsProvider,
            ILogger<FunctionAuth0Authenticator> logger)
        {
            this.secretsProvider = secretsProvider ?? throw new ArgumentNullException(nameof(secretsProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task<Auth0Authenticator> InitializeAuthenticatorAsync(bool allowExpiredToken, CancellationToken cancellationToken)
        {
            var domain = await this.secretsProvider.GetSecretAsync(SecretKeys.Auth0.Domain, cancellationToken);
            var audience = await this.secretsProvider.GetSecretAsync(SecretKeys.Auth0.ApiIdentifier, cancellationToken);
            return new Auth0Authenticator(domain, new[] {audience}, allowExpiredToken);
        }

        public async Task<IUserRefreshToken> RefreshTokenAsync(
            HttpRequest request,
            string refreshToken,
            CancellationToken cancellationToken)
        {
            this.authenticator ??= await this.InitializeAuthenticatorAsync(true, cancellationToken);
            if (this.authenticator == null)
                throw new NullReferenceException("Authenticator failed to initialize.");

            // We don't really need the info about user, but we want to make sure
            // token was valid before it expired so we authenticate without lifetime validation
            await this.authenticator.AuthenticateAsync(request, cancellationToken);

            // Request new token
            var domainTask = this.secretsProvider.GetSecretAsync(SecretKeys.Auth0.Domain, cancellationToken);
            var clientSecretTask = this.secretsProvider.GetSecretAsync(SecretKeys.Auth0.ClientSecretBeacon, cancellationToken);
            var clientIdTask = this.secretsProvider.GetSecretAsync(SecretKeys.Auth0.ClientIdBeacon, cancellationToken);
            await Task.WhenAll(domainTask, clientSecretTask, clientIdTask);

            var refreshTokenUrl = $"https://{domainTask.Result}{RefreshTokenUrlPath}";
            using var response = await new HttpClient().PostAsync(refreshTokenUrl, new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("grant_type", refreshToken),
                    new KeyValuePair<string, string>("client_id", clientIdTask.Result),
                    new KeyValuePair<string, string>("client_secret", clientSecretTask.Result),
                    new KeyValuePair<string, string>("refresh_token", "oldToken")
                }), cancellationToken);

            var tokenResult = await response.Content.ReadAsAsync<Auth0RefreshTokenResult>(cancellationToken);
            if (tokenResult == null)
                throw new Exception("Didn't get token.");
            if (string.IsNullOrWhiteSpace(tokenResult.AccessToken))
                throw new Exception("Got invalid access token - null or whitespace.");

            return new Auth0UserRefreshToken(
                tokenResult.AccessToken,
                DateTimeOffset.FromUnixTimeSeconds(tokenResult.ExpiresIn ?? 60).DateTime);
        }

        public async Task<IUserAuth> AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken)
        {
            try
            {
                this.authenticator ??= await this.InitializeAuthenticatorAsync(false, cancellationToken);
                if (this.authenticator == null)
                    throw new NullReferenceException("Authenticator failed to initialize.");
                
                var (user, _) = await this.authenticator.AuthenticateAsync(request, cancellationToken);
                var nameIdentifier = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(nameIdentifier))
                    throw new AuthenticationExpectedHttpException("NameIdentifier claim not present.");

                return new UserAuth(nameIdentifier);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Authorization failed");
                throw new AuthenticationExpectedHttpException(ex.Message);
            }
        }

        private class Auth0RefreshTokenResult
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int? ExpiresIn { get; set; }

            [JsonPropertyName("scope")]
            public string? Scope { get; set; }

            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }
        }

        private class Auth0UserRefreshToken : IUserRefreshToken
        {
            public Auth0UserRefreshToken(string accessToken, DateTime expire)
            {
                this.AccessToken = accessToken;
                this.Expire = expire;
            }
            
            public string AccessToken { get; }

            public DateTime Expire { get; }
        }
    }
}