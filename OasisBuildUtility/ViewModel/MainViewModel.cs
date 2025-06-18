using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Text.RegularExpressions;

namespace OasisBuildUtility.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private string _javaSourcePath = "";
        private string _nativeSourcePath = "";
        private string _logText = "Ready to build...\n";
        private bool _isBuildRunning = false;
        private int _progressValue = 0;
        private string _operationStatus = "Ready";
        private readonly DispatcherQueue _dispatcherQueue;

        #endregion

        #region Commands

        // Command for selecting Java source folder
        public ICommand SelectJavaSourceCommand { get; }

        // Command for selecting Native source file
        public ICommand SelectNativeSourceCommand { get; }

        // Command to start the build process
        public ICommand StartBuildCommand { get; }

        // Command to clear the log output
        public ICommand ClearLogCommand { get; }

        #endregion

        #region Public Properties

        // Path selected for Java source folder
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

        // Path selected for native source file
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

        // Text log output shown to the user
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

        // Boolean indicating if build is currently running
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

        // Current progress value (0–100)
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

        // String showing current operation status
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

        // Computed property: text shown on build button
        public string BuildButtonText => IsBuildRunning ? "BUILDING..." : "START BUILD";

        // Computed property: progress text label
        public string ProgressText => $"Progress: {ProgressValue}%";

        #endregion

        #region Constructor

        public MainViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Initialize commands with corresponding actions
            SelectJavaSourceCommand = new RelayCommand(async () => await SelectJavaSourcePathAsync());
            SelectNativeSourceCommand = new RelayCommand(async () => await SelectNativeSourcePathAsync());
            StartBuildCommand = new RelayCommand(async () => await BuildButtonAsync(), () => !IsBuildRunning);
            ClearLogCommand = new RelayCommand(ClearLog);

            AppendLogText("Build utility ready. Select your paths and start building.");
        }

        #endregion

        #region Path Selection Methods

        // Opens a folder picker to choose Java source path
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

        // Opens a file picker to choose Native source file
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

        #endregion

        #region Build Process Execution

        // Starts the build process if not already running
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

        // Runs the batch process with filtered output to exclude directory prompts
        private async Task RunBuildProcessAsync()
        {
            Process process = null;
            int exitCode = -1;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"C:\\Users\\Dell\\OneDrive\\Documents\\HelloWorld.bat\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process = new Process { StartInfo = startInfo };

                // Output handler with directory filtering
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // Filter out directory prompts and empty lines
                        if (!IsDirectoryPrompt(e.Data) && !string.IsNullOrWhiteSpace(e.Data))
                        {
                            _dispatcherQueue.TryEnqueue(() => AppendLogText(e.Data));
                        }
                    }
                };

                // Error handler
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _dispatcherQueue.TryEnqueue(() => AppendLogText($"ERROR: {e.Data}"));
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
                process?.Dispose();
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressValue = 100;
                AppendLogText($"Build process completed with exit code: {exitCode}");
                OperationStatus = exitCode == 0 ? "Build Completed" : "Build Failed";
            });
        }

        // Helper method to identify and filter out directory prompts
        private bool IsDirectoryPrompt(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true;

            var trimmedLine = line.Trim();

            // Filter out any line that contains a drive letter and ends with >
            if (Regex.IsMatch(trimmedLine, @"^[A-Za-z]:[^>]*>.*$"))
                return true;

            // Filter out lines that start with a path and contain >
            if (trimmedLine.Contains(":\\") && trimmedLine.Contains(">"))
                return true;

            // Filter out separator lines (lines with only dashes or equals)
            if (Regex.IsMatch(trimmedLine, @"^[=\-\s]+$"))
                return true;

            // Filter out lines that look like command prompts (more aggressive)
            if (trimmedLine.Contains("System32>") ||
                trimmedLine.Contains("Windows>") ||
                trimmedLine.Contains("Users>") ||
                trimmedLine.Contains("Documents>"))
                return true;

            return false;
        }

        #endregion

        #region Helper Methods

        // Appends text to the log with timestamp
        public void AppendLogText(string text)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                LogText += $"[{DateTime.Now:HH:mm:ss}] {text}\n";
            });
        }

        // Clears the log and resets progress/status
        public void ClearLog()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                LogText = "Log cleared.\n";
                ProgressValue = 0;
                OperationStatus = "Ready";
            });
        }

        // Raises CanExecuteChanged on the start build command
        private void RaiseCanExecuteChanged()
        {
            if (StartBuildCommand is RelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
