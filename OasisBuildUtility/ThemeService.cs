using Microsoft.UI.Xaml;
using Windows.Storage;
using System;

namespace OasisBuildUtility.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public class ThemeService
    {
        private const string THEME_SETTING_KEY = "AppTheme";
        private static ThemeService? _instance;
        private readonly ApplicationDataContainer _localSettings;

        public static ThemeService Instance => _instance ??= new ThemeService();

        private ThemeService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        public AppTheme CurrentTheme
        {
            get
            {
                if (_localSettings.Values[THEME_SETTING_KEY] is string themeString)
                {
                    if (Enum.TryParse<AppTheme>(themeString, out var theme))
                    {
                        return theme;
                    }
                }
                return AppTheme.System; // Default to system theme
            }
            set
            {
                _localSettings.Values[THEME_SETTING_KEY] = value.ToString();
                ApplyTheme(value);
            }
        }

        public void Initialize()
        {
            ApplyTheme(CurrentTheme);
        }

        public void ToggleTheme()
        {
            CurrentTheme = CurrentTheme switch
            {
                AppTheme.Light => AppTheme.Dark,
                AppTheme.Dark => AppTheme.Light,
                AppTheme.System => GetSystemTheme() == ElementTheme.Light ? AppTheme.Dark : AppTheme.Light,
                _ => AppTheme.System
            };
        }

        private void ApplyTheme(AppTheme theme)
        {
            if (App.MainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme switch
                {
                    AppTheme.Light => ElementTheme.Light,
                    AppTheme.Dark => ElementTheme.Dark,
                    AppTheme.System => ElementTheme.Default,
                    _ => ElementTheme.Default
                };
            }
        }

        private ElementTheme GetSystemTheme()
        {
            // This is a simplified way to get system theme
            // In a real app, you might want to use the UISettings API
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);

            // If background is dark (low RGB values), system is in dark mode
            return (color.R + color.G + color.B) < 384 ? ElementTheme.Dark : ElementTheme.Light;
        }

        public string GetCurrentThemeDisplayName()
        {
            return CurrentTheme switch
            {
                AppTheme.Light => "Light",
                AppTheme.Dark => "Dark",
                AppTheme.System => "System",
                _ => "Unknown"
            };
        }

        public string GetThemeIcon()
        {
            return CurrentTheme switch
            {
                AppTheme.Light => "\uE706", // Sun icon
                AppTheme.Dark => "\uE708",  // Moon icon
                AppTheme.System => "\uE7F4", // Auto icon
                _ => "\uE706"
            };
        }
    }
}