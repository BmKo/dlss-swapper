﻿using DLSS_Swapper.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DLSS_Swapper
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public ElementTheme GlobalElementTheme { get; set; }

        MainWindow _window;
        public MainWindow MainWindow => _window;

        public static App CurrentApp => (App)Application.Current;

        internal DLSSRecords DLSSRecords { get; } = new DLSSRecords();
        internal List<DLSSRecord> ImportedDLSSRecords { get; } = new List<DLSSRecord>();

        internal HttpClient _httpClient = new HttpClient();
        public HttpClient HttpClient => _httpClient;
        //public ObservableRangeCollection<DLSSRecord> CurrentDLSSRecords { get; } = new ObservableRangeCollection<DLSSRecord>();


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            Logger.Init();

            var version = GetVersion();
            var versionString = String.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);


            Logger.Info($"App launch - v{versionString}", null);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"dlss-swapper v{versionString}");

            GlobalElementTheme = Settings.Instance.AppTheme;

            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }


        internal void LoadLocalRecordFromDLSSRecord(DLSSRecord dlssRecord, bool isImportedRecord = false)
        {
#if PORTABLE
            var dllsPath = Path.Combine("StoredData", (isImportedRecord ? "imported_dlss_zip" : "dlss_zip"));
#else
            var dllsPath = Path.Combine(Storage.GetStorageFolder(), (isImportedRecord ? "imported_dlss_zip" : "dlss_zip"));
#endif

            var expectedPath = Path.Combine(dllsPath, $"{dlssRecord.Version}_{dlssRecord.MD5Hash}.zip");
            
            // Load record.
            var localRecord = LocalRecord.FromExpectedPath(expectedPath, isImportedRecord);

            if (isImportedRecord)
            {
                localRecord.IsImported = true;
                localRecord.IsDownloaded = true;
            }

            // If the record exists we will update existing properties, if not we add it as new property.
            if (dlssRecord.LocalRecord == null)
            {
                dlssRecord.LocalRecord = localRecord;
            }
            else
            {
                dlssRecord.LocalRecord.UpdateFromNewLocalRecord(localRecord);
            }
        }

        /*
        // Disabled because the non-async method seems faster.
        internal async Task LoadLocalRecordFromDLSSRecordAsync(DLSSRecord dlssRecord)
        {
            var expectedPath = Path.Combine("dlls", $"{dlssRecord.Version}_{dlssRecord.MD5Hash}", "nvngx_dlss.dll");
            Logger.Debug($"ExpectedPath: {expectedPath}");
            // Load record.
            var localRecord = await LocalRecord.FromExpectedPathAsync(expectedPath);

            // If the record exists we will update existing properties, if not we add it as new property.
            var existingLocalRecord = LocalRecords.FirstOrDefault(x => x.Equals(localRecord));
            if (existingLocalRecord == null)
            {
                dlssRecord.LocalRecord = localRecord;
                LocalRecords.Add(localRecord);
            }
            else
            {
                existingLocalRecord.UpdateFromNewLocalRecord(localRecord);

                // Probably don't need to set this again.
                dlssRecord.LocalRecord = existingLocalRecord;
            }
        }
        */

        internal void LoadLocalRecords()
        {
            // We attempt to load all local records, even if experemental is not enabled.
            foreach (var dlssRecord in DLSSRecords.Stable)
            {
                LoadLocalRecordFromDLSSRecord(dlssRecord);
            }
            foreach (var dlssRecord in DLSSRecords.Experimental)
            {
                LoadLocalRecordFromDLSSRecord(dlssRecord);
            }
            foreach (var dlssRecord in ImportedDLSSRecords)
            {
                LoadLocalRecordFromDLSSRecord(dlssRecord, true);
            }
        }


        /*
        // Disabled because the non-async method seems faster. 
        internal async Task LoadLocalRecordsAsync()
        {
            var tasks = new List<Task>();

            // We attempt to load all local records, even if experemental is not enabled.
            foreach (var dlssRecord in DLSSRecords.Stable)
            {
                tasks.Add(LoadLocalRecordFromDLSSRecordAsync(dlssRecord));
            }
            foreach (var dlssRecord in DLSSRecords.Experimental)
            {
                tasks.Add(LoadLocalRecordFromDLSSRecordAsync(dlssRecord));
            }
            await Task.WhenAll(tasks);
        }
        */



        internal bool IsRunningAsAdministrator()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /*
        // Disabled as I am unsure how to prompt to run as admin.
        internal void RelaunchAsAdministrator()
        {
            //var currentExe = Process.GetCurrentProcess().MainModule.FileName;

            //var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            //executingAssembly.FullName;
            
            // So this does prompt UAC, this was temporarily used to copy files in UpdateDll and ResetDll
            // but it would prompt for every action. 
            //var startInfo = new ProcessStartInfo()
            //{
            //    WindowStyle = ProcessWindowStyle.Hidden,
            //    FileName = "cmd.exe",
            //    Arguments = $"/C copy \"{dll}\" \"{targetDllPath}\"",
            //    UseShellExecute = true,
            //    Verb = "runas",
            //};
            //Process.Start(startInfo);

            MainWindow.Close();
            //Logger.Error(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
        */



        public Version GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
}
