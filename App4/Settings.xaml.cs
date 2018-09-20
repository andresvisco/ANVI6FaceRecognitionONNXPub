using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace App5
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        Windows.Storage.ApplicationDataContainer localSettings =
       Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder =
            Windows.Storage.ApplicationData.Current.LocalFolder;
        public Settings()
        {
            this.InitializeComponent();

            this.txtKey.Text = localSettings.Values["apiKey"]as string;
            this.txtKeyCV.Text = localSettings.Values["apiKeyCV"] as string;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            
            localSettings.Values["apiKey"] = this.txtKey.Text.ToString();
            localSettings.Values["apiKeyCV"] = this.txtKeyCV.Text.ToString();

        }
    }
}
