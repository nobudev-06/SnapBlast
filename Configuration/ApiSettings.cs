namespace SnapBlast.Configuration;

public static class ApiSettings
{
#if ANDROID
    public const string BaseUrl = "https://10.0.2.2:7280/";
#elif WINDOWS
    public const string BaseUrl = "https://localhost:7280/";
#else
    public const string BaseUrl = "https://localhost:7280/";
#endif
}