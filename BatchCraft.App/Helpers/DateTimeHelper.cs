namespace BatchCraft.App.Helpers;

public static class DateTimeHelper
{
    public static string ToDisplayTime(DateTime value) => value.ToString("HH:mm:ss");
}
