namespace SnapBlast.Configuration;

public static class ApiSettings
{
#if ANDROID
    // Android emulator: 10.0.2.2 maps to the development computer.
    public const string BaseUrl = "https://10.0.2.2:7280/";
#elif WINDOWS
    // Windows app: localhost points to the API running on this computer.
    public const string BaseUrl = "https://localhost:7280/";
#else
    // iOS simulator and Mac Catalyst commonly reach the development computer through localhost.
    public const string BaseUrl = "https://localhost:7280/";
#endif
}
