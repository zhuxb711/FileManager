﻿using RX_Explorer.Class;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace RX_Explorer
{
    public sealed partial class BlueScreen : Page
    {
        public BlueScreen()
        {
            InitializeComponent();
            Window.Current.SetTitleBar(TitleBar);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e?.Parameter is string ExceptionMessage)
            {
                Message.Text = ExceptionMessage;
            }
        }

        private async void Report_Click(object sender, RoutedEventArgs e)
        {
            _ = await Launcher.LaunchUriAsync(new Uri("mailto:zrfcfgs@outlook.com?subject=BugReport&body=" + Uri.EscapeDataString(Message.Text)));
        }

        private async void ExportLog_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker Picker = new FileSavePicker
            {
                SuggestedFileName = "Export_Error_Log.txt",
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            Picker.FileTypeChoices.Add(Globalization.GetString("File_Type_TXT_Description"), new List<string> { ".txt" });

            if (await Picker.PickSaveFileAsync() is StorageFile PickedFile)
            {
                await LogTracer.ExportLogAsync(PickedFile).ConfigureAwait(false);
            }
        }
    }
}
