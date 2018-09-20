using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using Windows;
using Windows.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;

namespace Microsoft.ProjectOxford.Face.Contract
{
    class UIHelper
    {
        public static Tuple<int, int> GetImageInfoForRendering(SoftwareBitmap imageFile)
        {
            try
            {
                return new Tuple<int, int>(imageFile.PixelWidth, imageFile.PixelHeight);
            }
            catch
            {
                return new Tuple<int, int>(0, 0);
            }
        }
        //public static IEnumerable<Face> CalculateFaceRectangleForRendering(IEnumerable<Microsoft.ProjectOxford.Face.Contract.Face> faces, int maxSize, Tuple<int, int> imageInfo)
        //{
        //    var imageWidth = imageInfo.Item1;
        //    var imageHeight = imageInfo.Item2;
        //    float ratio = (float)imageWidth / imageHeight;
        //    int uiWidth = 0;
        //    int uiHeight = 0;
        //    if (ratio > 1.0)
        //    {
        //        uiWidth = maxSize;
        //        uiHeight = (int)(maxSize / ratio);
        //    }
        //    else
        //    {
        //        uiHeight = maxSize;
        //        uiWidth = (int)(ratio * uiHeight);
        //    }

        //    int uiXOffset = (maxSize - uiWidth) / 2;
        //    int uiYOffset = (maxSize - uiHeight) / 2;
        //    float scale = (float)uiWidth / imageWidth;

            //foreach (var face in faces)
            //{
               /* yield return new Face()
                {
                    FaceId = face.FaceId.ToString(),
                    Left = (int)((face.FaceRectangle.Left * scale) + uiXOffset),
                    Top = (int)((face.FaceRectangle.Top * scale) + uiYOffset),
                    Height = (int)(face.FaceRectangle.Height * scale),
                    Width = (int)(face.FaceRectangle.Width * scale),
                };*/
        //    }
        //}
}
}
