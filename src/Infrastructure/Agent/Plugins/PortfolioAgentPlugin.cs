using System.ComponentModel;
using Application.Common.Security;
using Application.Portfolios.Interfaces;
using Microsoft.SemanticKernel;

namespace Infrastructure.Agent.Plugins;

public sealed record PortfolioSummaryForAgent(long Id, string Name, string Description);

/// <summary>
/// Kernel functions never take a userId parameter - that is exactly the injection vector the
/// zero-trust identity pattern avoids. Identity always comes from ICurrentUserProvider, which
/// resolves it from the authenticated JWT on the current request.
/// </summary>
public sealed class PortfolioAgentPlugin(
    IPortfolioRepository portfolioRepository,
    ICurrentUserProvider currentUserProvider)
{
    [KernelFunction, Description("Lists the authenticated user's own portfolios (id, name, description).")]
    public async Task<IReadOnlyList<PortfolioSummaryForAgent>> ListMyPortfoliosAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.UserId;

        var page = await portfolioRepository.GetAllAsync(
            page: 1,
            limit: 50,
            predicate: p => p.UserId == userId,
            cancellationToken);

        return page.Data
            .Select(p => new PortfolioSummaryForAgent(p.Id, p.Name, p.Description))
            .ToList();
    }

    [KernelFunction, Description("Gets the current total value (in the portfolio's own unit) of one of the authenticated user's portfolios, by portfolio id.")]
    public async Task<decimal?> GetPortfolioValueAsync(
        [Description("The portfolio id, as returned by ListMyPortfoliosAsync")] long portfolioId,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await portfolioRepository.GetByIdAsync(portfolioId, cancellationToken);

        // Ownership check: never trust an LLM-supplied id blindly, even though it only ever
        // came from this user's own ListMyPortfoliosAsync call in this conversation.
        if (portfolio is null || portfolio.UserId != currentUserProvider.UserId)
        {
            return null;
        }

        return portfolio.GetTotalValue(portfolio.Entries);
    }
}
