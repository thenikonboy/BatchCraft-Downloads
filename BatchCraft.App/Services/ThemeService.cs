using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BatchCraft.App.Services;

public sealed record AppTheme(
    string Id,
    bool IsDark,
    string NameTh,
    string NameEn,
    string Window,
    string Panel,
    string Input,
    string Text,
    string Muted,
    string Border,
    string Accent,
    string AccentHover,
    string AccentGlow,
    string Drop,
    string Status,
    string Success,
    string BadgeBg,
    string BadgeBorder,
    string BadgeText);

public static class ThemeService
{
    public const string DefaultThemeId = "midnight";
    public const string DefaultLightThemeId = "daylight";

    public static IReadOnlyList<AppTheme> All { get; } =
    [
        new(
            Id: "midnight",
            IsDark: true,
            NameTh: "มิดไนท์บลู",
            NameEn: "Midnight Blue",
            Window: "#0B0F19",
            Panel: "#111827",
            Input: "#151F32",
            Text: "#F8FAFC",
            Muted: "#94A3B8",
            Border: "#1E293B",
            Accent: "#2563EB",
            AccentHover: "#1D4ED8",
            AccentGlow: "#2563EB",
            Drop: "#0F172A",
            Status: "#0B1120",
            Success: "#10B981",
            BadgeBg: "#1E3A8A",
            BadgeBorder: "#3B82F6",
            BadgeText: "#60A5FA"),

        new(
            Id: "emerald",
            IsDark: true,
            NameTh: "เขียวมรกตหรู",
            NameEn: "Obsidian Emerald",
            Window: "#070D0B",
            Panel: "#0F1915",
            Input: "#15241E",
            Text: "#ECFDF5",
            Muted: "#86A89B",
            Border: "#1E352C",
            Accent: "#059669",
            AccentHover: "#047857",
            AccentGlow: "#10B981",
            Drop: "#0B1512",
            Status: "#08110E",
            Success: "#34D399",
            BadgeBg: "#064E3B",
            BadgeBorder: "#10B981",
            BadgeText: "#6EE7B7"),

        new(
            Id: "cyber",
            IsDark: true,
            NameTh: "ไซเบอร์ไวโอเล็ต",
            NameEn: "Cyber Violet",
            Window: "#0B0816",
            Panel: "#130F24",
            Input: "#1B1633",
            Text: "#F5F3FF",
            Muted: "#A59BC4",
            Border: "#2A224D",
            Accent: "#7C3AED",
            AccentHover: "#6D28D9",
            AccentGlow: "#8B5CF6",
            Drop: "#100C1E",
            Status: "#090613",
            Success: "#2DD4BF",
            BadgeBg: "#3B0764",
            BadgeBorder: "#A855F7",
            BadgeText: "#D8B4FE"),

        new(
            Id: "nordic",
            IsDark: true,
            NameTh: "นอร์ดิกสเลท",
            NameEn: "Nordic Slate",
            Window: "#19202B",
            Panel: "#222B3A",
            Input: "#2A3548",
            Text: "#F1F5F9",
            Muted: "#9BA9BE",
            Border: "#36445B",
            Accent: "#0284C7",
            AccentHover: "#0369A1",
            AccentGlow: "#38BDF8",
            Drop: "#1D2533",
            Status: "#161C26",
            Success: "#34D399",
            BadgeBg: "#0C4A6E",
            BadgeBorder: "#38BDF8",
            BadgeText: "#7DD3FC"),

        new(
            Id: "crimson",
            IsDark: true,
            NameTh: "โรสวูดคริมสัน",
            NameEn: "Rosewood Crimson",
            Window: "#110A0D",
            Panel: "#1B1115",
            Input: "#26181E",
            Text: "#FFF1F2",
            Muted: "#BC9AA3",
            Border: "#3B232C",
            Accent: "#E11D48",
            AccentHover: "#BE123C",
            AccentGlow: "#F43F5E",
            Drop: "#160D11",
            Status: "#0E080A",
            Success: "#34D399",
            BadgeBg: "#4C0519",
            BadgeBorder: "#FB7185",
            BadgeText: "#FDA4AF"),

        new(
            Id: "daylight",
            IsDark: false,
            NameTh: "สว่างคลีน",
            NameEn: "Pure Daylight",
            Window: "#F8FAFC",
            Panel: "#FFFFFF",
            Input: "#F1F5F9",
            Text: "#0F172A",
            Muted: "#64748B",
            Border: "#CBD5E1",
            Accent: "#2563EB",
            AccentHover: "#1D4ED8",
            AccentGlow: "#3B82F6",
            Drop: "#EFF6FF",
            Status: "#F1F5F9",
            Success: "#059669",
            BadgeBg: "#EFF6FF",
            BadgeBorder: "#93C5FD",
            BadgeText: "#1D4ED8"),

        new(
            Id: "sand",
            IsDark: false,
            NameTh: "กระดาษอุ่น",
            NameEn: "Warm Paper",
            Window: "#F8F4EC",
            Panel: "#FFFDF8",
            Input: "#F1EAE0",
            Text: "#29221A",
            Muted: "#786B5D",
            Border: "#DDD3C4",
            Accent: "#C2410C",
            AccentHover: "#9A3412",
            AccentGlow: "#EA580C",
            Drop: "#FDF6EC",
            Status: "#F2ECE1",
            Success: "#15803D",
            BadgeBg: "#FFEDD5",
            BadgeBorder: "#FDBA74",
            BadgeText: "#C2410C"),

        new(
            Id: "mint",
            IsDark: false,
            NameTh: "มิ้นต์พาสเทล",
            NameEn: "Mint Breeze",
            Window: "#F0FAF6",
            Panel: "#FFFFFF",
            Input: "#E4F5EE",
            Text: "#0F2922",
            Muted: "#52796F",
            Border: "#BEE3D4",
            Accent: "#0F766E",
            AccentHover: "#115E59",
            AccentGlow: "#14B8A6",
            Drop: "#E6F7F1",
            Status: "#E6F5EF",
            Success: "#059669",
            BadgeBg: "#CCFBF1",
            BadgeBorder: "#5EEAD4",
            BadgeText: "#0F766E")
    ];

    public static AppTheme Resolve(string? themeId)
    {
        var normalized = themeId?.Trim().ToLowerInvariant() switch
        {
            "dark" or null or "" => DefaultThemeId,
            "light" => DefaultLightThemeId,
            var id => id
        };
        return All.FirstOrDefault(t => t.Id == normalized) ?? All[0];
    }

    public static void Apply(ResourceDictionary resources, AppTheme theme)
    {
        SetBrush(resources, "WindowBrush", theme.Window);
        SetBrush(resources, "PanelBrush", theme.Panel);
        SetBrush(resources, "InputBrush", theme.Input);
        SetBrush(resources, "TextBrush", theme.Text);
        SetBrush(resources, "MutedBrush", theme.Muted);
        SetBrush(resources, "BorderBrush", theme.Border);
        SetBrush(resources, "AccentBrush", theme.Accent);
        SetBrush(resources, "AccentHoverBrush", theme.AccentHover);
        SetBrush(resources, "AccentTextBrush", "#FFFFFF");
        var shadow = new DropShadowEffect
        {
            Color = ParseColor(theme.AccentGlow),
            BlurRadius = 10,
            ShadowDepth = 2,
            Opacity = 0.4
        };
        shadow.Freeze();
        resources["TabActiveShadow"] = shadow;
        SetBrush(resources, "DropBrush", theme.Drop);
        SetBrush(resources, "StatusBrush", theme.Status);
        SetBrush(resources, "SuccessBrush", theme.Success);
        SetBrush(resources, "BadgeBgBrush", theme.BadgeBg);
        SetBrush(resources, "BadgeBorderBrush", theme.BadgeBorder);
        SetBrush(resources, "BadgeTextBrush", theme.BadgeText);
    }

    public static SolidColorBrush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush(ParseColor(hex));
        brush.Freeze();
        return brush;
    }

    public static System.Windows.Media.Color ParseColor(string hex) =>
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);

    private static void SetBrush(ResourceDictionary resources, string key, string hex) =>
        resources[key] = CreateBrush(hex);
}
