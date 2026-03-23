namespace BlazorApp.Services;

/// <summary>
/// Maintains a stable activity tag so UI->API calls can be correlated in traces and logs.
/// </summary>
public class ActivityTagService
{
    private string _currentTag = string.Empty;

    public string CurrentTag
    {
        get => _currentTag;
        private set => _currentTag = value;
    }

    public string GetOrCreate()
    {
        if (string.IsNullOrWhiteSpace(_currentTag))
            _currentTag = Guid.NewGuid().ToString("N");

        return _currentTag;
    }

    public void Set(string? tag)
    {
        _currentTag = string.IsNullOrWhiteSpace(tag) ? string.Empty : tag.Trim();
    }
}
