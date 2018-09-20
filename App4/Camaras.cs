using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace App5
{
    public class Camaras
    {
        public int Camara;

        public int CamaraId
        {
            get => default(int);
            set
            {
            }
        }
        public static List<Tuple<string, string>> listaCamaras = new List<Tuple<string, string>>();

        public static async Task<List<Tuple<string, string>>> ObtenerCamaras()
        {
            DeviceInformationCollection deviceInformation = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            foreach (var item in deviceInformation)
            {
                listaCamaras.Add(new Tuple<string, string>(item.Name, item.Id));
            }
            return listaCamaras;
        }
    }
}