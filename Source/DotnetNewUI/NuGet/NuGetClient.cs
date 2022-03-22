namespace DotnetNewUI.NuGet;

using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

internal interface INuGetClient
{
    Task<NuGetPackageInfo[]> GetNuGetTemplates();
}

internal class NuGetClient : INuGetClient
{
    private const int PageSize = 100;
    private readonly HttpClient httpClient;

    public NuGetClient(HttpClient httpClient)
        => this.httpClient = httpClient;

    public async Task<NuGetPackageInfo[]> GetNuGetTemplates()
    {
        var nuGetFeed = await GetNuGetFeed(NuGetUrlHelper.NuGetV3FeedUrl);
        var queryEndpoint = nuGetFeed.QueryUrl;
        var iconEndpoint = nuGetFeed.PackageIconUrl;

        var firstPageUrl = NuGetUrlHelper.GetTemplatePackageQueryFirstPageUrl(queryEndpoint, PageSize);
        var firstPage = await GetTemplatePackages(firstPageUrl);

        var remainingPagesUrls = NuGetUrlHelper.GetTemplatePackageQueryRemainingPagesUrls(queryEndpoint, firstPage.TotalHits, PageSize);
        var remainingPages = await Task.WhenAll(remainingPagesUrls.Select(url => GetTemplatePackages(url)));

        var allTemplates = Enumerable
            .Concat(firstPage.Data, remainingPages.SelectMany(p => p.Data))
            .ToArray();

        return allTemplates;
    }

    private async Task<NuGetFeed> GetNuGetFeed(string feedEndpoint)
    {
        var nuGetFeed = await this.httpClient.GetFromJsonAsync<NuGetFeed>(feedEndpoint);
        return nuGetFeed ?? throw new NullReferenceException("No response");
    }

    private async Task<NuGetQueryResponse> GetTemplatePackages(string url)
    {
        var queryResponse = await this.httpClient.GetFromJsonAsync<NuGetQueryResponse>(url);
        return queryResponse ?? throw new NullReferenceException("No response");
    }
}
