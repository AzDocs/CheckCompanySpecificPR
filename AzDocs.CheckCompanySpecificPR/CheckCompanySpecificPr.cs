using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AzDocs.CheckCompanySpecificPR.Models;
using AzDocs.CheckCompanySpecificPR.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;

namespace AzDocs.CheckCompanySpecificPR
{
    // todo: create deployments
    // Todo: write documentation 
    // Set up several webhooks (one for creation, one for update) 
    // add status policy to branch policies for branch
    // local.settings configuratie

    public class CheckCompanySpecificPr
    {
        private readonly IGitHubClientService _githubClientService;
        private readonly ICompareService _compareService;
        private readonly List<string> _companySpecificTerms;
        private readonly List<string> _acceptedTerms;

        public CheckCompanySpecificPr(IConfiguration configuration, IGitHubClientService githubClientService, ICompareService compareService)
        {
            _githubClientService = githubClientService;
            _compareService = compareService;
            _companySpecificTerms = configuration.GetSection("CompanySpecificTerms").AsEnumerable().Where(x => x.Value != null).Select(x => x.Value).ToList();
            _acceptedTerms = configuration.GetSection("AcceptedTerms").AsEnumerable().Where(x => x.Value != null).Select(x => x.Value).ToList();
        }

        [FunctionName("check-pr")]
        public async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req, ILogger logger)
        {
            var pr = new PullRequestInformation();
            try
            {
                logger.Log(LogLevel.Information, "Service Hook Received.");

                if (req.Content == null)
                {
                    logger.Log(LogLevel.Error, "The HttpRequestMessage does not have any content. Terminating.");
                    return req.CreateResponse(HttpStatusCode.BadRequest);
                }

                pr = JsonConvert.DeserializeObject<PullRequestInformation>(await req.Content.ReadAsStringAsync());

                if (!_companySpecificTerms.Any())
                {
                    logger.Log(LogLevel.Error, "No terms were specified. Please specify some company specific terms.");
                    await _githubClientService.SetStatusOfPrAsync(pr, GitStatusState.Failed,
                        "Failed because this check was enabled, but no company specific terms were added to the check. Please rectify this.");
                    return req.CreateResponse(HttpStatusCode.InternalServerError);
                }

                await ValidatePr(pr, _companySpecificTerms, _acceptedTerms);
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (JsonException ex)
            {
                logger.Log(LogLevel.Error, ex.ToString());
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.ToString());
                await _githubClientService.SetStatusOfPrAsync(pr, GitStatusState.Failed, "Failed because this check was enabled, but an error was thrown. Check the logs for more information.");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        private async Task ValidatePr(PullRequestInformation pr, List<string> companySpecificTerms, List<string> acceptedTerms)
        {
            var iterationsToCompare = await _githubClientService.GetPullRequestIterationsToCompareAsync(pr);
            var compare = await _githubClientService.GetPullRequestIterationChangesAsync(pr, iterationsToCompare);

            // get branch name
            GitVersionDescriptor gitDes = new GitVersionDescriptor { Version = pr.Resource.SourceRefName };

            // only check for distinct files
            var paths = compare.ChangeEntries.Select(x => x.Item.Path).Distinct();
            var found = false;
            foreach (var path in paths)
            {
                var file = await _githubClientService.GetPullRequestItemAsync(pr, path);
                if (file.Content == null) continue;

                foreach (var term in companySpecificTerms)
                {
                    var foundTerms = _compareService.FindCompanySpecificTerm(file.Content, term, path, acceptedTerms);
                    if (foundTerms.Any())
                    {
                        found = true;
                        foreach (var foundTerm in foundTerms)
                        {
                            var message = $"The company specific term {foundTerm.WordToFind} was found in this file for word {foundTerm.WordFound}. Please fix this.";
                            await _githubClientService.CreateThreadOnPrAsync(message, CommentThreadStatus.Active, CommentType.CodeChange, pr, foundTerm);
                        }
                    }
                }
            }

            if (found)
            {
                // found terms, setting status to failed
                await _githubClientService.SetStatusOfPrAsync(pr, GitStatusState.Failed, $"Failed because one or multiple company specific terms were used. Check the comments for the specific file.");
                return;
            }

            await _githubClientService.SetStatusOfPrAsync(pr, GitStatusState.Succeeded, "Check passed successfully.");
        }
    }
}
