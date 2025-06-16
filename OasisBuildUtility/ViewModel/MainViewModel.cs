using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OasisBuildUtility.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _javaSourcePath = "";
        private string _nativeSourcePath = "";
        private string _logText = "Ready to build...\n";
        private bool _isBuildRunning = false;
        private int _progressValue = 0;
        private string _operationStatus = "Ready";
        private readonly DispatcherQueue _dispatcherQueue;


        public ICommand SelectJavaSourceCommand { get; }
        public ICommand SelectNativeSourceCommand { get; }
        public ICommand StartBuildCommand { get; }
        public ICommand ClearLogCommand { get; }

        // Properties
        public string JavaSourcePath
        {
            get => _javaSourcePath;
            set
            {
                if (_javaSourcePath != value)
                {
                    _javaSourcePath = value;
                    OnPropertyChanged();
                    AppendLogText($"Java source path set to: {value}");
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
                    AppendLogText($"Native source path set to: {value}");
                }
            }
        }

        public string LogText
        {
            get => _logText;
            set
            {
                if (_logText != value)
                {
                    _logText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBuildRunning
        {
            get => _isBuildRunning;
            set
            {
                if (_isBuildRunning != value)
                {
                    _isBuildRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BuildButtonText));
                    RaiseCanExecuteChanged();
                }
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public string OperationStatus
        {
            get => _operationStatus;
            set
            {
                if (_operationStatus != value)
                {
                    _operationStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        // Computed properties
        public string BuildButtonText => IsBuildRunning ? "BUILDING..." : "START BUILD";
        public string ProgressText => $"Progress: {ProgressValue}%";

        public MainViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Initialize commands
            SelectJavaSourceCommand = new RelayCommand(async () => await SelectJavaSourcePathAsync());
            SelectNativeSourceCommand = new RelayCommand(async () => await SelectNativeSourcePathAsync());
            StartBuildCommand = new RelayCommand(async () => await BuildButtonAsync(), () => !IsBuildRunning);
            ClearLogCommand = new RelayCommand(ClearLog);

            AppendLogText("Build utility ready. Select your paths and start building.");
        }

        public async Task SelectJavaSourcePathAsync()
        {
            try
            {
                AppendLogText("Opening Java source folder picker...");
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
                else
                {
                    AppendLogText("Java source path selection cancelled");
                }
            }
            catch (Exception ex)
            {
                AppendLogText($"Error selecting Java source path: {ex.Message}");
            }
        }

        public async Task SelectNativeSourcePathAsync()
        {
            try
            {
                AppendLogText("Opening native source file picker...");
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
                else
                {
                    AppendLogText("Native source file selection cancelled");
                }
            }
            catch (Exception ex)
            {
                AppendLogText($"Error selecting native source path: {ex.Message}");
            }
        }

        public async Task BuildButtonAsync()
        {
            if (IsBuildRunning) return;

            try
            {
                IsBuildRunning = true;
                OperationStatus = "Building...";
                ProgressValue = 0;

                AppendLogText("Starting build process...");

                await RunBuildProcessAsync();
            }
            catch (Exception ex)
            {
                AppendLogText($"Error running build: {ex.Message}");
                OperationStatus = "Build Error";
            }
            finally
            {
                IsBuildRunning = false;
            }
        }

        private async Task RunBuildProcessAsync()
        {
            Process process = null;
            int exitCode = -1;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c ipconfig", // 🔧 Replaced with `ipconfig
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process = new Process { StartInfo = startInfo };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            AppendLogText(e.Data);
                        });
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            AppendLogText($"ERROR: {e.Data}");
                        });
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
                exitCode = process.ExitCode;
            }
            catch (Exception ex)
            {
                AppendLogText($"Process error: {ex.Message}");
                exitCode = -1;
            }
            finally
            {
                try
                {
                    process?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressValue = 100;
                AppendLogText($"Build process completed with exit code: {exitCode}");
                OperationStatus = exitCode == 0 ? "Build Completed" : "Build Failed";
            });
        }

        public void AppendLogText(string text)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                LogText += $"[{DateTime.Now:HH:mm:ss}] {text}\n";
            });
        }

        public void ClearLog()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                LogText = "Log cleared.\n";
                ProgressValue = 0;
                OperationStatus = "Ready";
            });
        }

        private void RaiseCanExecuteChanged()
        {
            if (StartBuildCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
