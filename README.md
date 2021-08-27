# Introduction

This application has been created as an extra validator for your Upstream.AzDocs project.

When using the Upstream.AzDocs project for your deployments, you might want to contribute to the project by adding or updating functionality.
To make sure you don't, by accident, push a change to the public repository that has any company specific information in it, we've created this function app that can check your pull requests on certain company specific terms and that notifies you of this mistake. Next to that, it is possible to add certain accepted terms, if you're sure these terms are valid.

Notifications wil be created based upon 'comments' that will be added to your pull request on creation and on the update of your pull request. Next to that, we have also added an optional status check to make sure, that even when you override these comments (by resolving the comments without fixing the issues) you are still notified that something isn't correct with your pull request.

# Getting Started

When you want to use this function app, make sure to deploy it inside your Azure Environment. For more information, see the chapter about 'Pipeline'.
The information below describes which settings you need to configure for your function app:

Create a Personal Access Token (PAT) to be able to connect to your Azure Devops environment:

1. Go to 'Personal Access Tokens'

   ![Personal Access Token](/readme_images/get-pat.png)]

2. Click 'New Token'
3. Create a new PAT with the following permissions:

   - Code -> Read&Write
   - Code -> Status

   ![Create Personal Access Token](/readme_images/create-pat.png)

4. Copy the PAT created
5. Go to your pipeline and create a pipeline variable by clicking on variables and add a new one with the name 'PAT' and paste the PAT (make sure to configure it as a secret)

![Pipeline variable PAT](/readme_images/pipeline-variable-pat.png)

6. Go to your pipeline and create a pipeline variable by clicking on variables and add a new one with the name 'Organization' and add the organization name of your Azure Devops environment

![Pipeline variable organization](/readme_images/pipeline-variable-organization.png)

7. Next to the settings mentioned above, you will have to update the App Settings of the function app with the terms you would like to be notified of, for example "ThisIsMyCompany". If there are any terms you DO accept, you can add these to the Accepted terms, for example "Project".

```yaml
- name: FunctionAppAppSettings
  value: "@('PAT=$(PAT)';'Organization=$(Organization)';'CompanySpecificTerms__0=ThisIsMyCompany';'CompanySpecificTerms__1=CompanyNameTest';'AcceptedTerms__0=Project';)"
```

In your Azure Devops environment, you will have to setup the following:

- Create a webhook for pull requests created on the main branch.
- Create a webhook for pull requests being updated on the main branch.
- Configure several policies:
  - Enable the 'Check for comment resolution' policy
  - Create an optional Status Check for the pull request

## Creating webhooks

### Pull Requests created

1. Go to the Project Settings of your project in Azure Devops.
2. Click on "Service Hooks".

![Service hooks](/readme_images/project-settings-service-hooks.png)

3. Add a new Service Hook and pick "Web hooks".

![Web hook](/readme_images/web-hook.png)

4. Add a trigger for Pull Requests created, see below:

![Web hook trigger](/readme_images/web-hook-trigger-pull-request-created.png)

5. Add the url you have setup your function to (note: the function is configured with function authorization, so make sure to copy the entire path).

![Web hook action](/readme_images/web-hook-action.png)

6. If you are using an Application Gateway, make sure to add a specific request header to your call from Azure Devops (this will be an extra check for the WAF).

![Web hook action header](/readme_images/web-hook-action-header.png)

7.  Click on "Finish"

### Pull Requests updated

1. Go to the Project Settings of your project in Azure Devops.
2. Click on "Service Hooks".

![Service hooks](/readme_images/project-settings-service-hooks.png)

3. Add a new Service Hook and pick "Web Hooks".

![Web hook](/readme_images/web-hook.png)

4. Add a trigger for Pull Requests updated, see below:

![Web hook trigger](/readme_images/web-hook-trigger-pull-request-updated.png)

5. Add the url you have setup your function to (note: the function is configured with function authorization, so make sure to copy the entire path).

![Web hook action](/readme_images/web-hook-action.png)

6. If you are using an Application Gateway, make sure to add a specific request header to your call from Azure Devops (this will be an extra check for the WAF).

![Web hook action header](/readme_images/web-hook-action-header.png)

7. Click on "Finish".

## Setting policies

### Policy Comments

1. Go to your 'Upstream.Azure.PlatformProvisioning' repository in your Azure Devops environment.
2. Click on 'Branches'.
3. Go to your `main` branch and pick your Branch Policies.

![Branch policies](/readme_images/branch-policies.png)

4. Enable the policy 'Check for comment resolution' and make this one required.

![Branch policies comment setting](/readme_images/branch-policies-setting-comment.png)

You have now enabled that comments need to be resolved, before you can complete a pull request.

### Policy Pull Request Status

1. Go to your 'Upstream.Azure.PlatformProvisioning' repository in your Azure Devops environment.
2. Click on 'Branches'.
3. Go to your `main` branch and pick your Branch Policies.

![Branch policies](/readme_images/branch-policies.png)

4. Go to 'Status Checks' and add a new one.

![Branch policies status check](/readme_images/branch-policies-status.png)

5. Add the following information for your status check and put this status check on 'Optional'.

![Branch policies status set](/readme_images/branch-policies-set-status.png)

## Pipeline

A pipeline has been added to the project as an example. _NOTE: The pipeline does expect you to have the base resources like a Log Analytics Workspace already setup and that you have general knowledge of the AzDocs project._

This pipeline consists of the following:

- pipeline-orchestrator.yml - your main entry point.
- pipeline-build.yml - the build of the function app.

Then you have several options:

- Deploy the function app, including VNet whitelisting and creating an entrypoint in the Application Gateway.
  - pipeline-release-including-vnet.yml
- Deploy the function app, without VNet whitelisting.
  - pipeline-release-public.yml

### Pipeline with VNet and Application Gateway

The pipeline is created with the scripts from the AzDocs project [AzDocs](https://github.com/AzDocs/AzDocs).

It makes use of the networking option of VNet whitelisting and has several steps included to be able to setup this pipeline behind your Application Gateway (for more information about Networking, see [Networking](https://github.com/AzDocs/AzDocs/blob/main/Wiki/Azure/Documentation/Networking.md)) An extra step has been added to be able to allow your request to be approved by the WAF (because of URLs Azure Devops includes in its request to your webhook), we have configured this based upon the hostname and based upon an extra header you can add to your webhooks, for more information on the script that is used go to [Set-ApplicationGatewayFirewallWhitelistRule](https://github.com/AzDocs/AzDocs/blob/main/Wiki/Azure/Azure-CLI-Snippets/Application-Gateway/Set-ApplicationGatewayFirewallWhitelistRule.md).

The extra header is specified in the pipeline with the following variables:

```yaml
- name: ApplicationGatewayWafCustomRuleRequestHeader
  value: "<your-request-header>"
- name: ApplicationGatewayWafCustomRuleRequestHeaderValue
  value: "<your-request-header-value>"
```

### Pipeline public

If you don't make use of VNets and the Application Gateway, a pipeline is also created that makes use of the scripts of the AzDocs project, but deploys your function app publically.

### Troubleshooting

In case you have configured all the above steps and deployed the function by using the pipeline-examples, but your status check stays in 'waiting', please check the following:

- Go to your Application Insights and check the logs on any errors.
- Make sure your PAT hasn't expired, if it is, go to the 'Getting Started' section.

## Contribute

If you would like to contribute to this project or make your own version, it is possible to run this project locally. This can be done by doing the following:

- Checkout the project
- Add a local.settings.json file to the application with the following information:

```json{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet"
  },
  "PAT": "insert_your_pat_here",
  "Organization": "insert_your_organization_here",
  "CompanySpecificTerms": [
    "ThisIsMyCompany",
    "CompanyTest"
  ],
  "AcceptedTerms": [
    "Company"
  ]
}
```

Run the project and start contributing! :)
