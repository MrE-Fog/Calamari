﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Calamari.Azure;
using Calamari.AzureWebAppZip;
using Calamari.Common.Plumbing.FileSystem;
using Calamari.Tests.Shared;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using NUnit.Framework;
using Sashimi.Tests.Shared.Server;

namespace Sashimi.AzureWebAppZip.Tests
{
    [TestFixture]
    class DeployAzureWebZipCommandFixture
    {
        private string _clientId;
        private string _clientSecret;
        private string _tenantId;
        private string _subscriptionId;
        private string _webappName;
        private string _resourceGroupName;
        private ResourceGroupsOperations _resourceGroupClient;
        private IList<DirectoryInfo> _tempDirs;

        [OneTimeSetUp]
        public async Task Setup()
        {
            _resourceGroupName = Guid.NewGuid().ToString();
            _tempDirs = new List<DirectoryInfo>();

            _clientId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionClientId);
            _clientSecret = ExternalVariables.Get(ExternalVariable.AzureSubscriptionPassword);
            _tenantId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionTenantId);
            _subscriptionId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionId);
            var resourceGroupLocation = Environment.GetEnvironmentVariable("AZURE_NEW_RESOURCE_REGION") ?? "eastus";

            var token = await GetAuthToken(_tenantId, _clientId, _clientSecret);

            var resourcesClient = new ResourcesManagementClient(_subscriptionId,
                new ClientSecretCredential(_tenantId, _clientId, _clientSecret));

            _resourceGroupClient = resourcesClient.ResourceGroups;

            var resourceGroup = new ResourceGroup(resourceGroupLocation);
            resourceGroup = await _resourceGroupClient.CreateOrUpdateAsync(_resourceGroupName, resourceGroup);

            var webMgmtClient = new WebSiteManagementClient(new TokenCredentials(token)) { SubscriptionId = _subscriptionId };

            var svcPlan = await webMgmtClient.AppServicePlans.BeginCreateOrUpdateAsync(resourceGroup.Name,
                resourceGroup.Name, new AppServicePlan(resourceGroup.Location));

            var webapp = await webMgmtClient.WebApps.BeginCreateOrUpdateAsync(resourceGroup.Name, resourceGroup.Name,
                new Site(resourceGroup.Location) { ServerFarmId = svcPlan.Id });

            _webappName = webapp.Name;
        }

        [OneTimeTearDown]
        public async Task Cleanup()
        {
            foreach (var tempDir in _tempDirs)
            {
                tempDir.Delete(true);
            }

            await _resourceGroupClient.StartDeleteAsync(_resourceGroupName);
        }

        [Test]
        public async Task Deploy_WebAppZip()
        {

            var tempPath = TemporaryDirectory.Create();
            _tempDirs.Add(new DirectoryInfo(tempPath.DirectoryPath));
            new DirectoryInfo(tempPath.DirectoryPath).CreateSubdirectory("AzureZipDeployPackage");
            await File.WriteAllTextAsync(Path.Combine($"{tempPath.DirectoryPath}/AzureZipDeployPackage", "index.html"),
                "Hello World");
            ZipFile.CreateFromDirectory($"{tempPath.DirectoryPath}/AzureZipDeployPackage",
                $"{tempPath.DirectoryPath}/AzureZipDeployPackage.1.0.0.zip");

            ActionHandlerTestBuilder.CreateAsync<ActionHandler, Program>()
                .WithArrange(context =>
                {
                    AddDefaults(context, _webappName);
                    context.WithPackage($"{tempPath.DirectoryPath}/AzureZipDeployPackage.1.0.0.zip",
                        "AzureZipDeployPackage", "1.0.0");
                })
                .Execute(runOutOfProc: true);
        }

        private async Task<string> GetAuthToken(string tenantId, string applicationId, string password)
        {
            var activeDirectoryEndPoint = @"https://login.windows.net/";
            var managementEndPoint = @"https://management.azure.com/";
            var authContext = GetContextUri(activeDirectoryEndPoint, tenantId);
            //Log.Verbose($"Authentication Context: {authContext}");
            var context = new AuthenticationContext(authContext);
            var result = await context.AcquireTokenAsync(managementEndPoint,
                new ClientCredential(applicationId, password));

            return result.AccessToken;
        }

        string GetContextUri(string activeDirectoryEndPoint, string tenantId)
        {
            if (!activeDirectoryEndPoint.EndsWith("/"))
            {
                return $"{activeDirectoryEndPoint}/{tenantId}";
            }

            return $"{activeDirectoryEndPoint}{tenantId}";
        }

        void AddDefaults(TestActionHandlerContext<Program> context, string webAppName)
        {
            context.Variables.Add(AccountVariables.ClientId, _clientId);
            context.Variables.Add(AccountVariables.Password, _clientSecret);
            context.Variables.Add(AccountVariables.TenantId, _tenantId);
            context.Variables.Add(AccountVariables.SubscriptionId, _subscriptionId);
            context.Variables.Add("Octopus.Action.Azure.ResourceGroupName", _resourceGroupName);
            context.Variables.Add("Octopus.Action.Azure.WebAppName", webAppName);
        }
    }
}
