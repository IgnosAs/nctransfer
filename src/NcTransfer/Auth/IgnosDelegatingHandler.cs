using System.Net.Http.Headers;
using Microsoft.Identity.Client;

namespace NcTransfer.Auth;

internal class IgnosDelegatingHandler : DelegatingHandler
{
    private readonly string[] scopes;

    private readonly IPublicClientApplication clientApp;

    public IgnosDelegatingHandler(string tenantId, string clientId, string scope)
    {
        scopes = new[] { scope };

        clientApp = PublicClientApplicationBuilder.Create(clientId)
            .WithTenantId(tenantId)
            .WithDefaultRedirectUri()
            .Build();

        TokenCacheHelper.EnableSerialization(clientApp.UserTokenCache);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accounts = await clientApp.GetAccountsAsync();
        var firstAccount = accounts.FirstOrDefault();

        AuthenticationResult? authResult;
        try
        {
            authResult = await clientApp.AcquireTokenSilent(scopes, firstAccount)
                .ExecuteAsync(cancellationToken);
        }
        catch (MsalUiRequiredException)
        {
            try
            {
                authResult = await clientApp.AcquireTokenInteractive(scopes)
                    .WithAccount(accounts.FirstOrDefault())
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();
            }
            catch (MsalException msalex)
            {
                throw new Exception($"Error Acquiring Token:{Environment.NewLine}{msalex}", msalex);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error Acquiring Token Silently:{Environment.NewLine}{ex}", ex);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
