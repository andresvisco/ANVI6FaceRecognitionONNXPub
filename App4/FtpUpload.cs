using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace App5
{
    class FtpUpload
    {
        public static string GroupId
        {
            get
            {
                return "1";
            }

            set
            {
                sampleGroupId = value;
            }
        }
        private static string sampleGroupId = Guid.NewGuid().ToString();

        public static async Task<bool> SubirArchivo(Stream imagenStream, Guid userGuid)
        {

            Windows.Storage.ApplicationDataContainer localSettings =
    Windows.Storage.ApplicationData.Current.LocalSettings;
            Windows.Storage.StorageFolder localFolder =
                Windows.Storage.ApplicationData.Current.LocalFolder;

            string subscriptionKey = localSettings.Values["apiKey"] as string;
            string subscriptionEndpoint = "https://southcentralus.api.cognitive.microsoft.com/face/v1.0";
            var faceServiceClient = new FaceServiceClient(subscriptionKey, subscriptionEndpoint);

            var faces = await faceServiceClient.AddPersonFaceInLargePersonGroupAsync(GroupId, userGuid, imagenStream,null,null );

           
            return true;

        }
        

        



    }
}
