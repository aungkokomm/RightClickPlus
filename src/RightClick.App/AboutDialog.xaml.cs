using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace RightClick_App;

public sealed partial class AboutDialog : ContentDialog
{
    public AboutDialog() => InitializeComponent();

    public string VersionText
    {
        get
        {
            var info = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var version = info?.Split('+')[0]
                ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "1.0.0";
            return $"Version {version}";
        }
    }
}
