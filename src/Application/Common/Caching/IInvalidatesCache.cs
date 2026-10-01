namespace Application.Common.Caching;

public interface IInvalidatesCache
{
    IReadOnlyCollection<string> CacheTagsToInvalidate { get; }
}
