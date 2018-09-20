using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace App5
{
    class HayConectividad
    {
        public static bool Conectividad()
        {
            var networkAdapters = NetworkInterface.GetAllNetworkInterfaces();
            if (networkAdapters.Length>0)
            {

                var statusNuevoEth = NetworkInformation.GetInternetConnectionProfile();
                if (statusNuevoEth != null)
                {
                    var statusNuevoEthValidado = statusNuevoEth.GetNetworkConnectivityLevel();
                    if (statusNuevoEthValidado.ToString() == "InternetAccess")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                //.GetNetworkConnectivityLevel();

                   

                
            }
            else
            {
                return false;
            }
            


        }
    }
}
