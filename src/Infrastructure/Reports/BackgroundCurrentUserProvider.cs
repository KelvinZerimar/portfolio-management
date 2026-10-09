using Application.Common.Security;

namespace Infrastructure.Reports;

// Registered only by Worker's AddReportScheduling, not by the shared AddInfrastructure. Dozens of
// Application handlers (every Portfolio/Exchange/CryptoCurrency/User query and command) depend on
// ICurrentUserProvider - normally only resolved via WebApi's HttpContext-backed implementation.
// MediatR registers all of them regardless of process (handlers are scanned from the shared
// Application assembly), and ASP.NET Core's strict DI validation (ValidateOnBuild/ValidateScopes,
// which only runs under Development) fails the whole host if ICurrentUserProvider can't be
// constructed at all - even though Worker never actually sends any of those requests. This stub
// only exists to satisfy that structural check; UserId should never actually be read in Worker.
internal sealed class BackgroundCurrentUserProvider : ICurrentUserProvider
{
    public long UserId => throw new NotSupportedException(
        "There is no current user in the Worker process. This code path should only run for an " +
        "authenticated HTTP request (handled by WebApi.MinimalAPI's CurrentUserProvider), never " +
        "from the background status-report sweep.");
}
