using System.Diagnostics;

namespace Api.Middleware;

/// <summary>
/// Reads X-Activity-Tag from incoming requests and enriches traces and logs.
/// </summary>
public class ActivityTagMiddleware(RequestDelegate next, ILogger<ActivityTagMiddleware> logger)
{
    public const string HeaderName = "X-Activity-Tag";
    public const string TagName = "activity.tag";

    public async Task Invoke(HttpContext context)
    {
        var tag = context.Request.Headers[HeaderName].ToString()?.Trim();
        var hasTag = !string.IsNullOrWhiteSpace(tag);

        Activity.Current?.SetTag("http.request.activity_tag_present", hasTag);
        if (hasTag)
            Activity.Current?.SetTag(TagName, tag);

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["ActivityTag"] = hasTag ? tag : string.Empty,
            ["ActivityTagPresent"] = hasTag
        }))
        {
            await next(context);
        }
    }
}
