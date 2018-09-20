using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace App5
{
    class CortarImagenesTageadas
    {
        public static ObservableCollection<Tuple<Windows.UI.Xaml.Media.ImageSource, Point, Size>> _items = new ObservableCollection<Tuple<Windows.UI.Xaml.Media.ImageSource, Point, Size>>();
        public static ObservableCollection<Tuple<Windows.UI.Xaml.Media.ImageSource, Point, Size>> Items
        {
            get { return _items; }
        }
        public static WriteableBitmap imgSource;
        public static async Task<Windows.UI.Xaml.Media.ImageSource> ImagenACortar(Point pointCaja, Size sizeCaja, StorageFile storageFile, double imageWidth)
        {
            if (storageFile != null)
            {
                using (IRandomAccessStream fileStream = await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    // Set the image source to the selected bitmap 
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.DecodePixelWidth = (int)imageWidth; //match the target Image.Width, not shown
                    await bitmapImage.SetSourceAsync(fileStream);
                    imgSource = await GetCroppedBitmapAsync.GetCroppedBitmapAsyncMethod(fileStream, pointCaja, new Size(30, 30), 1, 1);
                    Items.Add(new Tuple<Windows.UI.Xaml.Media.ImageSource, Point, Size>(imgSource, pointCaja, sizeCaja));
                }
                
            }
            return imgSource;
        }
    }
}
