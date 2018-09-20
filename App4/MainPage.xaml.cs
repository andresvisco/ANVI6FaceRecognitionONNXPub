using Windows.UI.Xaml.Controls;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Media.MediaProperties;
using Windows.Media.Capture;
using System.Threading.Tasks;
using System;
using Windows.System.Threading;v
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
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
using App5.Clases;
using Windows.AI.MachineLearning;

namespace App4
{


    public sealed partial class MainPage : Page
    {
        public static SoftwareBitmapSource imageSourceCW = new SoftwareBitmapSource();
        

        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        #region ClasesInicializadasPrincipal
        private VideoEncodingProperties videoProperties;
        private MediaCapture mediaCapture;
        private ThreadPoolTimer frameProcessingTimer;
        const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Bgra8;
        public InMemoryRandomAccessStream ms;
        public string SeleccionCamara = string.Empty;
        public string SeleccionCamaraID = string.Empty;
        IList<DetectedFace> faces = null;
        private FaceTracker faceTracker;
        private ScenarioState currentState;
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private readonly SolidColorBrush fillBrushText = new SolidColorBrush(Windows.UI.Colors.GreenYellow);
        private readonly SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.LimeGreen);
        private readonly double lineThickness = 2.0;
        private SemaphoreSlim frameProcessingSemaphore = new SemaphoreSlim(1);
        public string IdentidadEncontrada = "";

        public string PersonId
        {
            get
            {
                return _personId;
            }

            set
            {
                _personId = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("PersonId"));
                }
            }
        }

        #endregion ClasesInicializadasPrincipal

        #region Campos
        private static string sampleGroupId = Guid.NewGuid().ToString();
        private ObservableCollection<Face> _faces = new ObservableCollection<Face>();
        private ObservableCollection<Person> _persons = new ObservableCollection<Person>();
        private string _personId;

        public int MaxImageSize
        {
            get
            {
                return 300;
            }
        }
        #endregion Campos

        #region Constructor Main Page
        private async void cambioConection(object sender)
        {
            bool conectividad = HayConectividad.Conectividad();

            if (conectividad)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>{

                    btnIniciarStream.IsEnabled = true;
                    Notificaciones.MostrarNotificacion("Se recuperó la conexión");
                });
                
            }
            else
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {

                    btnIniciarStream.IsEnabled = false;
                    Notificaciones.MostrarNotificacion("Se perdió la conexión");
                });
            }
           
        }
        private const string _kModelFileName = "viscoreitano.onnx";//"perros.onnx";//
        private const string _kLabelsFileName = "Labels.json";
        public LearningModel _model = null;
        public LearningModelSession _session;
        private string identidadEncontradaTexto = string.Empty;
        public async Task<bool> IniciarModelo()
        {
            LearningModelDeviceKind GetDeviceKind()
            {
                return LearningModelDeviceKind.Default;
            }

            var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_kModelFileName}"));
            _model = await LearningModel.LoadFromStorageFileAsync(modelFile);
            _session = new LearningModelSession(_model, new LearningModelDevice(GetDeviceKind()));

            return true;
        }
        public MainPage()
        {
            
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["valorIdGroup"] = "1";
            this.InitializeComponent();
            NetworkInformation.NetworkStatusChanged += cambioConection;
            IniciarModelo().Wait(1000);
            
            ObtenerVideoDevices();
             

            




            if (HayConectividad.Conectividad())
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {

                 btnIniciarStream.IsEnabled = true;
                });
            }
            else
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {

                    btnIniciarStream.IsEnabled = false;
                });
            }


            listaCaras.ItemsSource = listCaras;
            if (localSettings.Values["apiKey"] as string =="")
            {
                this.btnIniciarStream.IsEnabled = false;

            }
            else
            {
                this.btnIniciarStream.IsEnabled = true;
            }
            ShutdownWebCam();
            btnTomarFoto.IsEnabled = false;
            this.currentState = ScenarioState.Idle;
            App.Current.Suspending += this.OnSuspending;
            
            App.Current.LeavingBackground += apagarCamara;
            




        }

        public List<Tuple<string, string>> listaCamaras;
        private async void ObtenerVideoDevices()
        {
          
            var camaras = await Camaras.ObtenerCamaras();
            listaCamaras = camaras.ToList<Tuple<string,string>>();
            lstBoxCamaras.ItemsSource = camaras;

        }

        public async void apagarCamara(object sender, LeavingBackgroundEventArgs e)
        {
            ShutdownWebCam();

        }
        #endregion Constructor


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

        
        private void CameraStreamingButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.TrackEvent("Click en IniciarStreamming");

            IdentidadEncontrada = "";
            if (this.currentState == ScenarioState.Streaming)
            {
                //this.rootPage.NotifyUser(string.Empty, NotifyType.StatusMessage);
                this.ChangeScenarioState(ScenarioState.Idle);
                btnIniciarStream.Content = "Iniciar Streamming";
            }
            else
            {

                //this.rootPage.NotifyUser(string.Empty, NotifyType.StatusMessage);
                this.ChangeScenarioState(ScenarioState.Streaming);
                btnIniciarStream.Content = "Parar Streamming";
            }
        }
        private void MediaCapture_CameraStreamFailed(MediaCapture sender, object args)
        {
            // MediaCapture is not Agile and so we cannot invoke its methods on this caller's thread
            // and instead need to schedule the state change on the UI thread.
            var ignored = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ChangeScenarioState(ScenarioState.Idle);
            });
        }
        
        private async Task<bool> StartWebcamStreaming()
        {
            var seleccioncamara = SeleccionCamaraID;
            

            bool successful = true;
            try
            {
                var camara = lstBoxCamaras.SelectedIndex;
                
                this.mediaCapture = new MediaCapture();
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Video;
                
                settings.VideoDeviceId = SeleccionCamaraID;
                



                await mediaCapture.InitializeAsync(settings);
                this.mediaCapture.Failed += this.MediaCapture_CameraStreamFailed;

                
                var deviceController = mediaCapture.VideoDeviceController;
                this.videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                this.CamPreview.Source = this.mediaCapture;
                await mediaCapture.StartPreviewAsync();

                TimeSpan timerInterval = TimeSpan.FromMilliseconds(88);
                this.frameProcessingTimer = Windows.System.Threading.ThreadPoolTimer.CreatePeriodicTimer(new Windows.System.Threading.TimerElapsedHandler(ProcessCurrentVideoFrame), timerInterval);
            }
            catch (System.UnauthorizedAccessException)
            {
                txtResult.Text = "El usuario no acepto utilizar su cámara";
                // If the user has disabled their webcam this exception is thrown; provide a descriptive message to inform the user of this fact.
                //this.rootPage.NotifyUser("Webcam is disabled or access to the webcam is disabled for this app.\nEnsure Privacy Settings allow webcam usage.", NotifyType.ErrorMessage);
                successful = false;
            }
            catch (Exception ex)
            {
                //this.rootPage.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
                successful = false;
            }
            return successful;

        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (this.faceTracker == null)
            {

                this.faceTracker = await FaceTracker.CreateAsync();
                
            }
        }

        private void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (this.currentState == ScenarioState.Streaming)
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                try
                {
                    this.ChangeScenarioState(ScenarioState.Idle);
                }
                finally
                {
                    deferral.Complete();
                }
            }
        }

        public async void ShutdownWebCam()
        {
            if (this.frameProcessingTimer != null)
            {
                this.frameProcessingTimer.Cancel();
            }

            if (this.mediaCapture != null)
            {
                if (this.mediaCapture.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
                {
                    try
                    {
                        await this.mediaCapture.StopPreviewAsync();
                    }
                    catch (Exception)
                    {
                        
                    }
                }
                this.mediaCapture.Dispose();
            }

            this.frameProcessingTimer = null;
            this.CamPreview.Source = null;
            this.mediaCapture = null;

        }
        private async void ChangeScenarioState(ScenarioState newState)
        {
            // Disable UI while state change is in progress
            switch (newState)
            {
                case ScenarioState.Idle:
                    

                    this.ShutdownWebCam();
                    btnTomarFoto.IsEnabled = false;

                    this.VisualizationCanvas.Children.Clear();
                    this.currentState = newState;
                    break;

                case ScenarioState.Streaming:

                    btnTomarFoto.IsEnabled = true;
                    if (!await this.StartWebcamStreaming())
                    {
                        this.ChangeScenarioState(ScenarioState.Idle);
                        break;
                    }

                    this.VisualizationCanvas.Children.Clear();
                    this.currentState = newState;

                    break;
            }
        }
        public SoftwareBitmapSource softwareBitmapSource = new SoftwareBitmapSource();



        private async void ProcessCurrentVideoFrame(ThreadPoolTimer timer)
        {

            if (this.currentState != ScenarioState.Streaming)
            {
                return;
            }

            if (!frameProcessingSemaphore.Wait(0))
            {
                return;
            }


            try
            {
                const BitmapPixelFormat InputPixelFormat1 = BitmapPixelFormat.Nv12;


                using (VideoFrame previewFrame = new VideoFrame(InputPixelFormat1, (int)this.videoProperties.Width, (int)this.videoProperties.Height))
                {
                    var valor = await this.mediaCapture.GetPreviewFrameAsync(previewFrame);
                    faces = await this.faceTracker.ProcessNextFrameAsync(previewFrame);

                    if (FaceDetector.IsBitmapPixelFormatSupported(previewFrame.SoftwareBitmap.BitmapPixelFormat))
                    {
                        var previewFrameSize = new Windows.Foundation.Size(previewFrame.SoftwareBitmap.PixelWidth, previewFrame.SoftwareBitmap.PixelHeight);
                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                        {
                            this.SetupVisualization(previewFrameSize, faces);

                        });
                        if (faces.Count != 0)
                        {
                            string nombre = "";
                            ObtenerIdentidadONNX obtenerIdentidadONNX = new ObtenerIdentidadONNX();
                            foreach (var caraEncontrad in faces)
                            {
                                if (IdentidadEncontrada == "")
                                {
                                    nombre = await obtenerIdentidadONNX.ObtenerIdentidadOnnX(previewFrame, _session);

                                  //  nombre = await App5.ObtenerIdentidad.ObtenerIdentidadAPI(valor, videoProperties, mediaCapture);

                                  
                                    

                                    if (nombre != "" )
                                    {
                                        IdentidadEncontrada = nombre;
                                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                        {
                                            txtIdentificando.Visibility = Visibility.Collapsed;
                                            RectIdentificando.Visibility = Visibility.Collapsed;
                                        });
                                        
                                        AgregarCaraALista(IdentidadEncontrada);
                                    }
                                    else
                                    {
                                        nombre = "No se encontro identidad";
                                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                                        {
                                            IdentidadEncontrada = string.Empty;
                                            txtIdentificando.Visibility = Visibility.Visible;
                                            RectIdentificando.Visibility = Visibility.Visible;
                                        });
                                    }
                                    
                                }                         


                            }
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                            {
                                txtResult.Text = nombre;
                                imagenCamaraWeb.Source = imageSourceCW;
                            });


                        }
                        else
                        {
                           
                            string nombre = string.Empty;
                            int contador = 0;
                            IdentidadEncontrada = string.Empty;

                        }

                    }
                    else
                    {
                        throw new System.NotSupportedException("PixelFormat '" + InputPixelFormat.ToString() + "' is not supported by FaceDetector");
                    }



                }
            }
            catch (Exception ex)
            {
                var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    txtResultServicio.Text = ex.Message.ToString();
                    //this.rootPage.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
                });
            }
            finally
            {
                frameProcessingSemaphore.Release();
            }

        }

        public ObservableCollection<NombrePersona> listCaras = new ObservableCollection<NombrePersona>();
        private async void AgregarCaraALista(string caraUsuario)
        {

            if (!listCaras.Any(x => x.Nombre.ToString() == caraUsuario))
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    NombrePersona nombrePersona = new NombrePersona(caraUsuario);

                    listCaras.Add(nombrePersona);

                });
            }

                
          
            

        }
        public class NombrePersona
        {
           
            public string Nombre
            {
                get; set;
            }
            public NombrePersona(string nombrePers)
            {
                this.Nombre= nombrePers;
            }
        }
        public async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
        {
            byte[] array = null;

            // First: Use an encoder to copy from SoftwareBitmap to an in-mem stream (FlushAsync)
            // Next:  Use ReadAsync on the in-mem stream to get byte[] array

            using (ms = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
                encoder.SetSoftwareBitmap(soft);

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception ex) { return new byte[0]; }

                array = new byte[ms.Size];
                await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
            }
            return array;
        }
       
        public MediaElement MediaElementThread = new MediaElement();
        public async Task<SoftwareBitmap> AsBitmapImage(byte[] byteArray)
        {
            if (byteArray != null)
            {
                using (var stream = new InMemoryRandomAccessStream())
                {
                    stream.WriteAsync(byteArray.AsBuffer()).GetResults();
                    // I made this one synchronous on the UI thread;
                    // this is not a best practice.
                    var image = new SoftwareBitmap(BitmapPixelFormat.Bgra8, 100, 100,BitmapAlphaMode.Premultiplied);
                    stream.Seek(0);

                    return image;
                }
            }

            return null;
        }


        public async Task<byte[]> ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        private enum ScenarioState
        {

            Idle,
            Streaming
        }
        public ImageBrush imageBrush = new ImageBrush();
        public ImageSource imageSource = new BitmapImage(new Uri("ms-appx:///Assets/unicornio.png"));
        public Image imageUnic = new Image();
        private void SetupVisualization(Windows.Foundation.Size framePizelSize, IList<DetectedFace> foundFaces)
        {
            this.VisualizationCanvas.Children.Clear();
            double actualWidth = this.VisualizationCanvas.ActualWidth;
            double actualHeight = this.VisualizationCanvas.ActualHeight;
            if (this.currentState == ScenarioState.Streaming && foundFaces != null && actualWidth != 0 && actualHeight != 0)
            {
                double widthScale = framePizelSize.Width / actualWidth;
                double heightScale = framePizelSize.Height / actualHeight;
                int i = 0;
                foreach (DetectedFace face in foundFaces)
                {
                    TextBlock texto = new TextBlock();
                    Rectangle box = new Rectangle();

                    
                        try
                        {
                            box.Name = "box" + IdentidadEncontrada.ToString();
                            box.Width = (int)face.FaceBox.Width / (int)widthScale;
                            box.Height = (int)(face.FaceBox.Height / heightScale);
                            box.Fill = this.fillBrush;
                            box.Stroke = this.lineBrush;
                            box.StrokeThickness = this.lineThickness;
                            box.Margin = new Thickness((uint)(face.FaceBox.X / widthScale), (uint)(face.FaceBox.Y / heightScale), 0, 0);

                            texto.Name = "text" + IdentidadEncontrada.ToString();
                            texto.Text = IdentidadEncontrada;
                            texto.Margin = new Thickness((uint)(face.FaceBox.X / widthScale), (uint)(face.FaceBox.Y / heightScale) - 15, 0, 0);
                            texto.Foreground = this.lineBrush;
                            this.VisualizationCanvas.Children.Add(box);

                            this.VisualizationCanvas.Children.Add(texto);

                        }
                        catch (Exception ex)
                        {
                            var error = ex.Message.ToString();
                            
                            
                        }
                        
                            
                }

            }

        }


        private async void btnTomarFoto_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {


            txtResult.Text = "";
            IBuffer buffer;
            var previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height,BitmapAlphaMode.Premultiplied);
            VideoFrame previewFrame = await mediaCapture.GetPreviewFrameAsync(videoFrame);
            SoftwareBitmap previewBitmap = previewFrame.SoftwareBitmap;
            var arrayByteData = await EncodedBytes(previewBitmap, BitmapEncoder.JpegEncoderId);

            SoftwareBitmap softwareBitmapCropped = await CreateFromBitmap(previewBitmap, (uint)previewBitmap.PixelWidth, (uint)previewBitmap.PixelHeight);



            SoftwareBitmapSource softwareBitmapSourceFoto = new SoftwareBitmapSource();
            await softwareBitmapSource.SetBitmapAsync(softwareBitmapCropped);
            var arrayCropped = await EncodedBytes(softwareBitmapCropped, BitmapEncoder.JpegEncoderId);



            Stream stream = arrayCropped.AsBuffer().AsStream();
            
            Altas.imageBytes = arrayByteData;
            Altas.imageStream = stream;
            Altas.softwareBitmapSource = softwareBitmapSource;

            previewFoto.Source = softwareBitmapSource;
            previewFrame.Dispose();
            this.currentState = ScenarioState.Idle;
            ShutdownWebCam();
            this.Frame.Navigate(typeof(Altas));








            // await ProcessCurrentVideoFrame();
        }

        public ObservableCollection<Face> TargetFaces
        {
            get
            {
                return _faces;
            }
        }



        public static async Task<SoftwareBitmap> CreateFromBitmap(SoftwareBitmap softwareBitmap, uint width, uint heigth)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);


                encoder.SetSoftwareBitmap(softwareBitmap);
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

                

                var ancho = width * (0.2);
                var alto = heigth * (0.2);

                
                encoder.BitmapTransform.ScaledWidth = (uint)ancho;
                encoder.BitmapTransform.ScaledHeight = (uint)alto;

                await encoder.FlushAsync();

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                return await decoder.GetSoftwareBitmapAsync(softwareBitmap.BitmapPixelFormat, softwareBitmap.BitmapAlphaMode);
            }
        }
        

        public event PropertyChangedEventHandler PropertyChanged;


        public ObservableCollection<Person> Persons
        {
            get
            {
                return _persons;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.currentState = ScenarioState.Idle;
            ShutdownWebCam();
            this.Frame.Navigate(typeof(Altas));

        }
        public Task obtenerAudio;

        

      


        private void VisualizationCanvas_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var punteroX = e.GetCurrentPoint(this.VisualizationCanvas).Position.X;
            var punteroY = e.GetCurrentPoint(this.VisualizationCanvas).Position.Y;

            
            txtResult.Text = punteroX.ToString() + " - " + punteroY.ToString();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void VersionVideo_Click(object sender, RoutedEventArgs e)
        {
            this.currentState = ScenarioState.Idle;
            ShutdownWebCam();
            this.Frame.Navigate(typeof(MainPageVideoSource));
        }
        

        private void lstBoxCamaras_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SeleccionCamara = ((Windows.UI.Xaml.Controls.Primitives.Selector)sender).SelectedItem.ToString();
            SeleccionCamaraID = ((System.Tuple<string, string>)((Windows.UI.Xaml.Controls.Primitives.Selector)sender).SelectedItem).Item2.ToString();
            ChangeScenarioState(ScenarioState.Idle);
            ChangeScenarioState(ScenarioState.Streaming);




        }
    }
}
