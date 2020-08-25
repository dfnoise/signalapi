﻿using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Signal.Core;

namespace Signal.Infrastructure.Secrets
{
    public class SecretsProvider : ISecretsProvider
    {
        private const string KeyVaultUrl = "https://signalapi.vault.azure.net/";

        public async Task<string> GetSecretAsync(string key)
        {
            var client = new SecretClient(
                new Uri(KeyVaultUrl),
                new DefaultAzureCredential());
            var secret = await client.GetSecretAsync(key);
            return secret.Value.Value;
        }
    }
}