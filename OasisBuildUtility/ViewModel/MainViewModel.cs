using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OasisBuildUtility.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _javaSourcePath;
        private string _nativeSourcePath;

        public string JavaSourcePath
        {
            get => _javaSourcePath;
            set
            {
                if (_javaSourcePath != value)
                {
                    _javaSourcePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NativeSourcePath
        {
            get => _nativeSourcePath;
            set
            {
                if (_nativeSourcePath != value)
                {
                    _nativeSourcePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SelectJavaSourceCommand { get; }
        public ICommand SelectNativeSourceCommand { get; }

        public MainViewModel()
        {
            SelectJavaSourceCommand = new RelayCommand(SelectJavaSourcePath);
            SelectNativeSourceCommand = new RelayCommand(SelectNativeSourcePath);
        }

        private async void SelectJavaSourcePath()
        {
            var picker = new FolderPicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                JavaSourcePath = folder.Path;
            }
        }

        private async void SelectNativeSourcePath()
        {
            var picker = new FileOpenPicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                NativeSourcePath = file.Path;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}