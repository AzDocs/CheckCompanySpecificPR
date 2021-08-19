using System;
using AzDocs.CheckCompanySpecificPR.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

[assembly: FunctionsStartup(typeof(AzDocs.CheckCompanySpecificPR.Startup))]
namespace AzDocs.CheckCompanySpecificPR
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;
            builder.Services.AddSingleton(s =>
            {
                var pat = configuration.GetValue<string>("PAT");
                var organization = configuration.GetValue<string>("Organization");

                VssConnection connection = new VssConnection(new Uri($"https://dev.azure.com/{organization}"), new VssBasicCredential(string.Empty, pat));
                return connection.GetClient<GitHttpClient>();
            });

            builder.Services.AddSingleton<IGitHttpClientWrapper, GitHttpClientWrapper>();
            builder.Services.AddTransient<IGitHubClientService, GitHubClientService>();
            builder.Services.AddTransient<ICompareService, CompareService>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional:true, reloadOnChange:true)
                .AddEnvironmentVariables();

            base.ConfigureAppConfiguration(builder);
        }
    }
}
