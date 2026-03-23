using System.Diagnostics;
using System.Net.Http.Headers;

namespace BlazorApp.Services;

/// <summary>
/// Adds X-Activity-Tag to outbound API requests for end-to-end correlation.
/// </summary>
public class ActivityTagHandler(ActivityTagService activityTagService) : DelegatingHandler
{
    public const string HeaderName = "X-Activity-Tag";
    public const string TagName = "activity.tag";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tag = activityTagService.GetOrCreate();
        request.Headers.Remove(HeaderName);
        request.Headers.Add(HeaderName, tag);

        Activity.Current?.SetTag(TagName, tag);
        Activity.Current?.SetTag("http.request.activity_tag_present", true);

        return base.SendAsync(request, cancellationToken);
    }
}
