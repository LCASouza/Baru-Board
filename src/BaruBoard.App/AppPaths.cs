namespace BaruBoard.App;

internal static class AppPaths
{
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BaruBoard");

    public static string RecoveryDirectory => Path.Combine(DataDirectory, "recovery");

    public static string RecentFilesIndex => Path.Combine(DataDirectory, "recent.json");
}
