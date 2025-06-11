using System;
using Microsoft.UI.Xaml;

namespace OasisBuildUtility
{
    public partial class App : Application
    {
        public static MainWindow MainWindow { get; set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
    }
}
