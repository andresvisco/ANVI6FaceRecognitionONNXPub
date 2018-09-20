using Windows.UI.Xaml.Controls;
using Microsoft.ProjectOxford.Face;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Media.MediaProperties;
using Windows.Media.Capture;
using System.Threading.Tasks;
using System;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.ProjectOxford.Face.Contract;
using System.ComponentModel;
using Windows.Media.FaceAnalysis;
using System.Collections.Generic;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using App5;
using Windows.Storage;
using Windows.Networking.Connectivity;
using Windows.ApplicationModel;
using Microsoft.AppCenter.Analytics;
using App4;

namespace App5
{
    public static class ObtenerIdentidad
    {
        const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Bgra8;
        public static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public static StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        public static async Task<string> ObtenerIdentidadAPI(VideoFrame videoFrame, VideoEncodingProperties videoProperties, MediaCapture mediaCapture)
        {
            byte[] arrayImage;
            var PersonName = "";

            videoFrame = new VideoFrame(InputPixelFormat, (int)videoProperties.Width, (int)videoProperties.Height);

            
            try
            {

                var valor = await mediaCapture.GetPreviewFrameAsync(videoFrame);

                SoftwareBitmap softwareBitmapPreviewFrame = valor.SoftwareBitmap;

                Size sizeCrop = new Size(softwareBitmapPreviewFrame.PixelWidth, softwareBitmapPreviewFrame.PixelHeight);
                Point point = new Point(0, 0);
                Rect rect = new Rect(0, 0, softwareBitmapPreviewFrame.PixelWidth, softwareBitmapPreviewFrame.PixelHeight);
                var arrayByteData = await EncodedBytesClass.EncodedBytes(softwareBitmapPreviewFrame, BitmapEncoder.JpegEncoderId);

                SoftwareBitmap softwareBitmapCropped = await MainPage.CreateFromBitmap(softwareBitmapPreviewFrame, (uint)softwareBitmapPreviewFrame.PixelWidth, (uint)softwareBitmapPreviewFrame.PixelHeight);
                SoftwareBitmap displayableImage = SoftwareBitmap.Convert(softwareBitmapCropped, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                //SoftwareBitmap displayableImageGray = SoftwareBitmap.Convert(softwareBitmapCropped, BitmapPixelFormat.Gray16);

                

                //await MainPage.imageSourceCW.SetBitmapAsync(displayableImage).AsTask();

                arrayImage = await EncodedBytesClass.EncodedBytes(displayableImage, BitmapEncoder.JpegEncoderId);

                var nuevoStreamFace = new MemoryStream(arrayImage);


                string subscriptionKey = localSettings.Values["apiKey"] as string;
                string subscriptionEndpoint = "https://southcentralus.api.cognitive.microsoft.com/face/v1.0";
                var faceServiceClient = new FaceServiceClient(subscriptionKey, subscriptionEndpoint);
                
                //var caraPersona = await faceServiceClient.GetPersonFaceAsync("1", )
                try
                {


                    // using (var fsStream = File.OpenRead(sampleFile))
                    // {
                    IEnumerable<FaceAttributeType> faceAttributes =
            new FaceAttributeType[] {
                FaceAttributeType.Gender,
                FaceAttributeType.Age,
                FaceAttributeType.Smile,
                FaceAttributeType.Emotion,
                FaceAttributeType.Glasses,
                FaceAttributeType.Hair };


                    var faces = await faceServiceClient.DetectAsync(nuevoStreamFace, true, false, faceAttributes);

                    string edad = string.Empty;
                    string genero = string.Empty;
                    string emocion = string.Empty;

                    var resultadoIdentifiacion = await faceServiceClient.IdentifyAsync(faces.Select(ff => ff.FaceId).ToArray(), largePersonGroupId: App4.MainPage.GroupId);

                    


                    for (int idx = 0; idx < faces.Length; idx++)
                    {
                        // Update identification result for rendering
                        edad = faces[idx].FaceAttributes.Age.ToString();
                        genero = faces[idx].FaceAttributes.Gender.ToString();

                        if (genero != string.Empty)
                        {
                            if (genero == "male")
                            {
                                genero = "Masculino";
                            }
                            else
                            {
                                genero = "Femenino";
                            }
                        }



                        var res = resultadoIdentifiacion[idx];

                        if (res.Candidates.Length > 0)
                        {
                            var nombrePersona = await faceServiceClient.GetPersonInLargePersonGroupAsync(App4.MainPage.GroupId, res.Candidates[0].PersonId);
                            PersonName = nombrePersona.Name.ToString();
                            //var estadoAnimo = 
                           
                        }
                        else
                        {
                            //txtResult.Text = "Unknown";
                        }
                    }
                    //}

                }
                catch (FaceAPIException ex)
                {
                    var error = ex.Message.ToString();
                    
                }

            }
            catch (Exception ex)
            {
                var mensaje = ex.Message.ToString();
                
            }
            return PersonName;

        }
    }
}
