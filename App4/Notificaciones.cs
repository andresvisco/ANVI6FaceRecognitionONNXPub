using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace App5
{
    class Notificaciones
    {
        public static void MostrarNotificacion(string textoNotificacion)
        {
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = textoNotificacion
                                },

                                new AdaptiveText()
                                {
                                    Text = textoNotificacion
                                }


                            }


                }
            };

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual
            };
            ToastNotification toastNotification = new ToastNotification(toastContent.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
        }
    
    }
}
