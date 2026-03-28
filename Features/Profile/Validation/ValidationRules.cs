using auth_template.Features.Profile.Enums;

namespace auth_template.Features.Profile.Validation;

public static class ValidationRules
{
    public static bool ValidateDomainMatch(LinkType type, string url)
    {
        var normalizedUrl = url.Trim();
        if (!normalizedUrl.StartsWith("http://") && !normalizedUrl.StartsWith("https://"))
        {
            normalizedUrl = "https://" + normalizedUrl;
        }
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? uri)) return false;
    
        var host = uri.Host.ToLowerInvariant();

        
        bool IsMatch(params string[] domains) 
            => domains.Any(d => host == d || host.EndsWith("." + d));
        return type switch
        {
            LinkType.Linkedin => IsMatch("linkedin.com"),
            LinkType.Youtube => IsMatch("youtube.com", "youtu.be"),
            LinkType.X => IsMatch("x.com", "twitter.com"),
            LinkType.Github => IsMatch("github.com"),
            LinkType.Gitlab => IsMatch("gitlab.com"),
            LinkType.Bitbucket => IsMatch("bitbucket.org"),
            LinkType.Behance => IsMatch("behance.net"),
            LinkType.Stackoverflow => IsMatch("stackoverflow.com"),
            LinkType.Medium => IsMatch("medium.com"),
            LinkType.Producthunt => IsMatch("producthunt.com"),
            LinkType.Hackernews => IsMatch("news.ycombinator.com"),
            LinkType.Wellfound => IsMatch("wellfound.com"),
            LinkType.Substack => IsMatch("substack.com"),
            _ => false 
        };
    }
    
}