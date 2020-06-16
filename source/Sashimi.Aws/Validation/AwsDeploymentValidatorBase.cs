﻿using System;
using FluentValidation;
using Sashimi.Server.Contracts;
using Sashimi.Server.Contracts.ActionHandlers.Validation;
using PropertiesDictionary = System.Collections.Generic.IReadOnlyDictionary<string, string>;

namespace Sashimi.Aws.Validation
{
    abstract class AwsDeploymentValidatorBase : IDeploymentActionValidator
    {
        readonly string actionType;

        protected AwsDeploymentValidatorBase(string actionType)
        {
            this.actionType = actionType;
        }

        protected bool ThisAction(DeploymentActionValidationContext arg)
            => arg.ActionType == actionType;

        public virtual void AddDeploymentValidationRule(AbstractValidator<DeploymentActionValidationContext> validator)
        {
            validator.RuleFor(a => a.Properties)
                .MustHaveProperty(AwsSpecialVariables.Action.Aws.AwsRegion, "Please provide the AWS region that the step will default to.")
                .When(ThisAction);

            validator.RuleFor(a => a.Properties)
                .MustHaveProperty(AwsSpecialVariables.Action.Aws.AssumedRoleArn, $"Please provide the assumed role ARN.")
                .When(ThisAction)
                .When(a => a.Properties.ContainsKey(AwsSpecialVariables.Action.Aws.AssumeRole) &&
                    "True".Equals(a.Properties[AwsSpecialVariables.Action.Aws.AssumeRole], StringComparison.OrdinalIgnoreCase));

            validator.RuleFor(a => a.Properties)
                .MustHaveProperty(AwsSpecialVariables.Action.Aws.AssumedRoleSession, $"Please provide the assumed role session name.")
                .When(ThisAction)
                .When(a => a.Properties.ContainsKey(AwsSpecialVariables.Action.Aws.AssumeRole) &&
                    "True".Equals(a.Properties[AwsSpecialVariables.Action.Aws.AssumeRole], StringComparison.OrdinalIgnoreCase));

            validator.RuleFor(a => a.Properties)
                .MustHaveProperty(AwsSpecialVariables.Action.Aws.AccountId, "Please specify the AWS account.")
                .When(ThisAction)
                .When(a => !IsInstanceRole(a.Properties));
        }

        protected static bool ScriptIsFromPackage(PropertiesDictionary properties)
        {
            return properties.TryGetValue(KnownVariables.Action.Script.ScriptSource, out var scriptSource) &&
                scriptSource == KnownVariableValues.Action.Script.ScriptSource.Package;
        }

        static bool IsInstanceRole(PropertiesDictionary properties)
        {
            return properties.ContainsKey(AwsSpecialVariables.Action.Aws.UseInstanceRole) &&
                (properties[AwsSpecialVariables.Action.Aws.UseInstanceRole] ?? "") == "True";
        }
    }
}