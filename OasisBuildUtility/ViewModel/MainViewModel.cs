using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
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

        // Commands
        public ICommand SelectJavaSourceCommand { get; }
        public ICommand SelectNativeSourceCommand { get; }
        public ICommand StartBuildCommand { get; }
        public ICommand StartJavaBuildCommand { get; }
        public ICommand ClearLogCommand { get; }

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
                    // Refresh command can execute states
                    ((RelayCommand)StartBuildCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)StartJavaBuildCommand).RaiseCanExecuteChanged();
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

        public string BuildButtonText => IsBuildRunning ? "BUILDING..." : "START BUILD";
        public string ProgressText => $"Progress: {ProgressValue}%";

        public MainViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Initialize commands
            SelectJavaSourceCommand = new RelayCommand(async () => await SelectJavaSourcePathAsync());
            SelectNativeSourceCommand = new RelayCommand(async () => await SelectNativeSourcePathAsync());
            StartBuildCommand = new RelayCommand(async () => await TestBuildAsync(), () => !IsBuildRunning);
            StartJavaBuildCommand = new RelayCommand(async () => await ExecuteJavaBuildAsync(), () => !IsBuildRunning);
            ClearLogCommand = new RelayCommand(ClearLog);

            AppendLogText("ViewModel initialized successfully");
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
                    AppendLogText($"Java source path selected: {folder.Path}");
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
                    AppendLogText($"Native source file selected: {file.Path}");
                }
                else
                {
                    AppendLogText("Native source file selection cancelled");
                }
            }
            catch (Exception ex)
            {
                AppendLogText($"Error selecting native source file: {ex.Message}");
            }
        }

        public async Task TestBuildAsync()
        {
            if (IsBuildRunning)
            {
                AppendLogText("Build already running, ignoring click");
                return;
            }

            // Validate paths before starting build
            if (string.IsNullOrWhiteSpace(JavaSourcePath))
            {
                AppendLogText("❌ ERROR: Java source path is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(NativeSourcePath))
            {
                AppendLogText("❌ ERROR: Native source path is required");
                return;
            }

            IsBuildRunning = true;
            OperationStatus = "Building Project";
            AppendLogText("=== BUILD PROCESS STARTED ===");
            AppendLogText($"Java Source: {JavaSourcePath}");
            AppendLogText($"Native Source: {NativeSourcePath}");
            AppendLogText("");

            try
            {
                // Step 1: Verify Java installation
                await RunBuildStep("Verifying Java installation...", "java -version", 10);

                // Step 2: Check if Maven/Gradle is available
                await RunBuildStep("Checking build tools...", "mvn --version", 20);

                // Step 3: Navigate to Java source directory and list contents
                await RunBuildStep("Analyzing Java source directory...", $"dir \"{JavaSourcePath}\"", 30);

                // Step 4: Look for build files (pom.xml, build.gradle, etc.)
                await RunBuildStep("Looking for build configuration...", $"dir \"{JavaSourcePath}\" | findstr /i \"pom.xml build.gradle\"", 40);

                // Step 5: Check native source
                await RunBuildStep("Analyzing native source...", $"dir \"{NativeSourcePath}\"", 50);

                // Step 6: Example Java compilation (if .java files exist)
                await RunBuildStep("Compiling Java sources...", $"cd /d \"{JavaSourcePath}\" && dir *.java", 70);

                // Step 7: Example native build preparation
                await RunBuildStep("Preparing native build...", $"cd /d \"{System.IO.Path.GetDirectoryName(NativeSourcePath)}\" && dir", 85);

                // Step 8: Final status
                await RunBuildStep("Build process completed", "echo Build finished successfully!", 100);

                OperationStatus = "Build Completed";
                AppendLogText("=== BUILD PROCESS COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                OperationStatus = "Build Failed";
                AppendLogText($"❌ BUILD FAILED: {ex.Message}");
                AppendLogText("=== BUILD PROCESS TERMINATED ===");
            }
            finally
            {
                IsBuildRunning = false;
            }
        }

        private async Task RunBuildStep(string stepName, string command, int progressTarget)
        {
            AppendLogText($"🔧 [{DateTime.Now:HH:mm:ss}] {stepName}");
            AppendLogText($"⚡ Executing: {command}");
            OperationStatus = stepName;

            try
            {
                var result = await ExecuteCommandWithRealTimeOutputAsync(command);

                AppendLogText($"📋 Exit Code: {result.ExitCode}");

                if (!string.IsNullOrWhiteSpace(result.Output))
                {
                    AppendLogText($"📤 Output:");
                    AppendLogText(result.Output);
                }

                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    AppendLogText($"⚠️ Error Output:");
                    AppendLogText(result.Error);
                }

                if (result.ExitCode == 0)
                {
                    AppendLogText("✅ Step completed successfully");
                }
                else
                {
                    AppendLogText($"⚠️ Step completed with exit code: {result.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                AppendLogText($"❌ Exception during step: {ex.Message}");
                AppendLogText($"🔍 Stack trace: {ex.StackTrace}");
            }

            // Update progress on UI thread
            _dispatcherQueue.TryEnqueue(() =>
            {
                ProgressValue = progressTarget;
            });

            AppendLogText(""); // Empty line for readability
            await Task.Delay(500); // Shorter delay for better responsiveness
        }

        private async Task<CommandResult> ExecuteCommandWithRealTimeOutputAsync(string command)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processInfo = new ProcessStartInfo()
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                        WorkingDirectory = Environment.CurrentDirectory
                    };

                    using var process = Process.Start(processInfo);
                    if (process == null)
                    {
                        return new CommandResult
                        {
                            Output = "",
                            Error = "Failed to start process",
                            ExitCode = -1
                        };
                    }

                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();

                    // Read output and error streams asynchronously for real-time display
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            outputBuilder.AppendLine(args.Data);
                            AppendLogText($"➤ {args.Data}");
                        }
                    };

                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            errorBuilder.AppendLine(args.Data);
                            AppendLogText($"⚠️ {args.Data}");
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    return new CommandResult
                    {
                        Output = outputBuilder.ToString().Trim(),
                        Error = errorBuilder.ToString().Trim(),
                        ExitCode = process.ExitCode
                    };
                }
                catch (Exception ex)
                {
                    AppendLogText($"❌ Process execution failed: {ex.Message}");
                    return new CommandResult
                    {
                        Output = "",
                        Error = $"Process execution failed: {ex.Message}",
                        ExitCode = -1
                    };
                }
            });
        }

        // Add method for executing custom build commands
        public async Task ExecuteCustomBuildCommandAsync(string customCommand)
        {
            if (IsBuildRunning)
            {
                AppendLogText("Build already running, cannot execute custom command");
                return;
            }

            if (string.IsNullOrWhiteSpace(customCommand))
            {
                AppendLogText("❌ ERROR: Custom command cannot be empty");
                return;
            }

            IsBuildRunning = true;
            OperationStatus = "Executing Custom Command";
            AppendLogText("=== CUSTOM BUILD COMMAND EXECUTION ===");
            AppendLogText($"Command: {customCommand}");
            AppendLogText("");

            try
            {
                await RunBuildStep("Executing custom build command...", customCommand, 100);
                OperationStatus = "Custom Command Completed";
                AppendLogText("=== CUSTOM COMMAND COMPLETED ===");
            }
            catch (Exception ex)
            {
                OperationStatus = "Custom Command Failed";
                AppendLogText($"❌ CUSTOM COMMAND FAILED: {ex.Message}");
            }
            finally
            {
                IsBuildRunning = false;
            }
        }

        // Method for actual Java build process
        public async Task ExecuteJavaBuildAsync()
        {
            if (IsBuildRunning)
            {
                AppendLogText("Build already running, ignoring request");
                return;
            }

            if (string.IsNullOrWhiteSpace(JavaSourcePath))
            {
                AppendLogText("❌ ERROR: Java source path is required for build");
                return;
            }

            IsBuildRunning = true;
            OperationStatus = "Building Java Project";
            AppendLogText("=== JAVA BUILD PROCESS STARTED ===");
            AppendLogText($"Java Source Directory: {JavaSourcePath}");
            AppendLogText("");

            try
            {
                // Check if it's a Maven project
                var pomPath = System.IO.Path.Combine(JavaSourcePath, "pom.xml");
                var buildGradlePath = System.IO.Path.Combine(JavaSourcePath, "build.gradle");

                if (System.IO.File.Exists(pomPath))
                {
                    AppendLogText("📦 Detected Maven project (pom.xml found)");
                    await RunBuildStep("Maven clean...", $"cd /d \"{JavaSourcePath}\" && mvn clean", 25);
                    await RunBuildStep("Maven compile...", $"cd /d \"{JavaSourcePath}\" && mvn compile", 50);
                    await RunBuildStep("Maven package...", $"cd /d \"{JavaSourcePath}\" && mvn package", 75);
                    await RunBuildStep("Maven install...", $"cd /d \"{JavaSourcePath}\" && mvn install", 100);
                }
                else if (System.IO.File.Exists(buildGradlePath))
                {
                    AppendLogText("🔧 Detected Gradle project (build.gradle found)");
                    await RunBuildStep("Gradle clean...", $"cd /d \"{JavaSourcePath}\" && gradle clean", 25);
                    await RunBuildStep("Gradle compileJava...", $"cd /d \"{JavaSourcePath}\" && gradle compileJava", 50);
                    await RunBuildStep("Gradle build...", $"cd /d \"{JavaSourcePath}\" && gradle build", 100);
                }
                else
                {
                    AppendLogText("⚠️ No Maven or Gradle build file detected, attempting manual compilation...");
                    await RunBuildStep("Creating build directory...", $"cd /d \"{JavaSourcePath}\" && mkdir build 2>nul", 20);
                    await RunBuildStep("Finding Java files...", $"cd /d \"{JavaSourcePath}\" && dir /s *.java", 40);
                    await RunBuildStep("Compiling Java files...", $"cd /d \"{JavaSourcePath}\" && javac -d build src\\*.java", 80);
                    await RunBuildStep("Creating JAR file...", $"cd /d \"{JavaSourcePath}\" && jar cf output.jar -C build .", 100);
                }

                OperationStatus = "Java Build Completed";
                AppendLogText("=== JAVA BUILD PROCESS COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                OperationStatus = "Java Build Failed";
                AppendLogText($"❌ JAVA BUILD FAILED: {ex.Message}");
                AppendLogText("=== JAVA BUILD PROCESS TERMINATED ===");
            }
            finally
            {
                IsBuildRunning = false;
            }
        }

        public void AppendLogText(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}\n";

            // Ensure UI updates happen on the UI thread
            _dispatcherQueue.TryEnqueue(() =>
            {
                LogText += logEntry;
            });
        }

        public void ClearLog()
        {
            LogText = "Log cleared.\n";
            ProgressValue = 0;
            OperationStatus = "Ready";
            AppendLogText("Log cleared by user");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}