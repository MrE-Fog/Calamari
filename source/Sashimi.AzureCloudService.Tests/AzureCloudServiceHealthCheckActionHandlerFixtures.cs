using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Sashimi.Server.Contracts.ActionHandlers;
using Sashimi.Tests.Shared.Server;

namespace Sashimi.AzureCloudService.Tests
{
    [TestFixture]
    public class AzureCloudServiceHealthCheckActionHandlerFixtures
    {
        [Test]
        public void Validate_Fails_If_Legacy_Account()
        {
            var handler = new AzureCloudServiceHealthCheckActionHandler();

            Action act = () => handler.WithArrange(variables =>
            {
                variables.Add(SpecialVariables.AccountType, "Boo");
                variables.Add(SpecialVariables.Action.Azure.AccountId, "myaccount");
            }).Execute();

            act.Should().Throw<KnownDeploymentFailureException>();
        }

        [Test]
        public void Validate_Fails_If_Wrong_Account_Type()
        {
            var handler = new AzureCloudServiceHealthCheckActionHandler();

            Action act = () => handler.WithArrange(variables =>
                {
                    variables.Add(SpecialVariables.AccountType, "Boo");
                })
                .Execute();

            act.Should().Throw<KnownDeploymentFailureException>();
        }
    }
}