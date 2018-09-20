
using Windows.UI.Xaml.Controls;
using Microsoft.ProjectOxford.Face;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Media.MediaProperties;
using Windows.Media.Capture;
using System.Threading.Tasks;
using System;
using Windows.System.Threading;
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
using Windows.Storage;
using Windows.Media.Core;
using MediaCaptureExtensions;
using Windows.Media.Editing;
using Windows.Media.Capture.Frames;
using Windows.Devices.Enumeration;
using System.Net.Http;
using System.Diagnostics;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App4
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 



    public sealed partial class MainPageVideoSource : Page
    {
        #region ClasesInicializadasPrincipal
        private VideoEncodingProperties videoProperties;
        private MediaCapture mediaCapture;
        private ThreadPoolTimer frameProcessingTimer;
        const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Bgra8;
        public InMemoryRandomAccessStream ms;

        IList<DetectedFace> faces = null;
        private FaceTracker faceTracker;
        private ScenarioState currentState;
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private readonly SolidColorBrush fillBrushText = new SolidColorBrush(Windows.UI.Colors.GreenYellow);
        private readonly SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);
        private readonly double lineThickness = 2.0;
        private SemaphoreSlim frameProcessingSemaphore = new SemaphoreSlim(1);
        public string IdentidadEncontrada = "";
        public IMediaSource mediaSourceVideo;


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
        public StorageFile storage;
        #region Constructor Main Page
        public MainPageVideoSource()
        {

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["valorIdGroup"] = "1";
            this.InitializeComponent();

            
            this.currentState = ScenarioState.Idle;
            App.Current.Suspending += this.OnSuspending;



        }
        #endregion Constructor


        public string GroupId
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


        //private async void btnTomarFoto_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        //{
        //    using (VideoFrame previewFrame = new VideoFrame(InputPixelFormat, (int)this.videoProperties.Width, (int)this.videoProperties.Height))
        //    {

        //    }
        //}
        private void CameraStreamingButton_Click(object sender, RoutedEventArgs e)
        {
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
            bool successful = true;
            try
            {





                this.mediaCapture = new MediaCapture();
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();


                settings.StreamingCaptureMode = StreamingCaptureMode.Video;
                await mediaCapture.InitializeAsync(settings);
                this.mediaCapture.Failed += this.MediaCapture_CameraStreamFailed;

                var deviceController = mediaCapture.VideoDeviceController;
                this.videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                this.CamPreview.Source = this.mediaCapture;
                await mediaCapture.StartPreviewAsync();

                TimeSpan timerInterval = TimeSpan.FromMilliseconds(99);
            }
            catch (System.UnauthorizedAccessException)
            {
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
        private async void ShutdownWebCam()
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
                        ;   // Since we're going to destroy the MediaCapture object there's nothing to do here
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

                    this.VisualizationCanvas.Children.Clear();
                    this.currentState = newState;
                    break;

                case ScenarioState.Streaming:


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

        
        public MediaClip clip;
        public StorageFile pickedFile;
        public TimeSpan timeSpan = new TimeSpan(00, 00, 00, 00, 00);
        public Stream streamImagePersist;
        public ImageStream imagestreamPublico;
        public List<Tuple<string, string, FaceRectangle>> listaCaras = new List<Tuple<string, string, FaceRectangle>>();

        private async void iniciarProceso(ThreadPoolTimer timer)
        {


            if (!frameProcessingSemaphore.Wait(0))
            {
                return;
            }
            try
            {


                while (timeSpan <= clip.OriginalDuration)
                {
                    timeSpan = timeSpan.Add(new TimeSpan(00, 00, 00, 03, 00));

                    List<string> encondingProperties = new List<string>();
                    encondingProperties.Add("System.Video.FrameHeight");
                    encondingProperties.Add("System.Video.FrameWidth");
                    IDictionary<string, object> encodingProperties = await pickedFile.Properties.RetrievePropertiesAsync(encondingProperties);
                    
                    var divisor = 0.5;
                    uint frameHeight = (uint)encodingProperties["System.Video.FrameHeight"];
                    uint frameWidth = (uint)encodingProperties["System.Video.FrameWidth"];

                    var valorHeight = frameHeight * divisor;
                    var valorWidth = frameWidth * divisor;


                    var composition = new MediaComposition();
                    composition.Clips.Add(clip);

                    var imageStream = await composition.GetThumbnailAsync(timeSpan, (int)valorWidth, (int)valorHeight, VideoFramePrecision.NearestFrame);
                    imagestreamPublico = await composition.GetThumbnailAsync(timeSpan, (int)valorWidth, (int)valorHeight, VideoFramePrecision.NearestFrame);


                    var StreamImage = imageStream.AsStream();
                    streamImagePersist = imageStream.AsStream();

                    Size size = new Size();


                    try
                    {
                        IEnumerable<FaceAttributeType> faceAttributes = new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Emotion, FaceAttributeType.Glasses, FaceAttributeType.Hair };

                        string subscriptionKey = "a6fa05b6601b4ea398aa2039d601d983";
                        string subscriptionEndpoint = "https://southcentralus.api.cognitive.microsoft.com/face/v1.0";
                        var faceServiceClient = new FaceServiceClient(subscriptionKey, subscriptionEndpoint);

                        var SizeStream = StreamImage.Length / 1024;

                        var facesNueva = await faceServiceClient.DetectAsync(streamImagePersist, false, true, faceAttributes);

                        var CantidadFaces = facesNueva.Length;
                                             


                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async () =>
                        {
                            if (CantidadFaces>0)
                            {
                                var Genero = string.Empty;
                                var Edad = string.Empty;
                                foreach (var face in facesNueva)
                                {
                                    try
                                    {
                                        size = new Size(valorWidth, valorHeight);
                                        Genero = face.FaceAttributes.Gender.ToString();
                                        Edad= face.FaceAttributes.Age.ToString();
                                        listaCaras.Add(new Tuple<string, string, FaceRectangle>("hola", "hola", face.FaceRectangle));

                                    }
                                    catch (FaceAPIException ex)
                                    {
                                        var error = ex.ErrorMessage.ToString() + " / " + ex.Message.ToString();
                                        txtResult.Text = error.ToString();
                                    }
                                }
                                SetupVisualization(size, imagestreamPublico, Genero, Edad, listaCaras);



                            }
                        });
                        StreamImage.Dispose();


                        



                        //string faceApiKey = "a6fa05b6601b4ea398aa2039d601d983";
                        //string faceApiEndPoint = "https://southcentralus.api.cognitive.microsoft.com/face/v1.0";
                        //HttpClient httpClient = new HttpClient();
                        //httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceApiKey);
                        //string requestParameters = "returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";
                        //string uri = faceApiEndPoint + "/detect?" + requestParameters;
                        //byte[] vs = await ReadFully(StreamImage);

                        //using (ByteArrayContent content = new ByteArrayContent(vs))
                        //{
                        //    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                        //    var response = httpClient.PostAsync(uri, content).Result;
                        //}

                    }
                    catch (FaceAPIException exFaceApi)
                    {
                        var error = exFaceApi.Message.ToString();
                        
                    }

                    composition.Clips.Remove(clip);
                }
            }
            catch (Exception ex)
            {
                var erro = ex.Message.ToString();

            }
            finally
            {
                frameProcessingSemaphore.Release();
            }
        }

        public async Task<bool> caraProcesada(Face[] cara)
        {
            var caraNueva = cara;

            return true;
        }
        private async Task<bool> BuscarCara(MemoryStream streamFace)
        {
            string subscriptionKey = "a6fa05b6601b4ea398aa2039d601d983";
            string subscriptionEndpoint = "https://southcentralus.api.cognitive.microsoft.com/face/v1.0";
            

            var faceServiceClient = new FaceServiceClient(subscriptionKey, subscriptionEndpoint);

            // {
            IEnumerable<FaceAttributeType> faceAttributes =
    new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Emotion, FaceAttributeType.Glasses, FaceAttributeType.Hair };
            try
            {
                var faces = await faceServiceClient.DetectAsync(streamFace,true, true);

                string edad = string.Empty;
                string genero = string.Empty;

                return true;
            }
            catch (Exception ex)
            {
                var error = ex.Message.ToString();
                throw;
            }

            
        }

        private async Task<byte[]> ConvertBitmapToByteArray(WriteableBitmap bitmap)
        {
            using (Stream stream = bitmap.PixelBuffer.AsStream())
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
        


        public async Task<string> ObtenerIdentidad()
        {
            byte[] arrayImage;
            var PersonName = "";


            try
            {
                const BitmapPixelFormat InputPixelFormat1 = BitmapPixelFormat.Bgra8;

                using (VideoFrame previewFrame = new VideoFrame(InputPixelFormat1, (int)this.videoProperties.Width, (int)this.videoProperties.Height))
                {
                    var valor = await this.mediaCapture.GetPreviewFrameAsync(previewFrame);


                

                    SoftwareBitmap softwareBitmapPreviewFrame = valor.SoftwareBitmap;

                    Size sizeCrop = new Size(softwareBitmapPreviewFrame.PixelWidth, softwareBitmapPreviewFrame.PixelHeight);
                    Point point = new Point(0, 0);
                    Rect rect = new Rect(0, 0, softwareBitmapPreviewFrame.PixelWidth, softwareBitmapPreviewFrame.PixelHeight);
                    var arrayByteData = await EncodedBytes(softwareBitmapPreviewFrame, BitmapEncoder.JpegEncoderId);

                    SoftwareBitmap softwareBitmapCropped = await CreateFromBitmap(softwareBitmapPreviewFrame, (uint)softwareBitmapPreviewFrame.PixelWidth, (uint)softwareBitmapPreviewFrame.PixelHeight);
                    SoftwareBitmap displayableImage = SoftwareBitmap.Convert(softwareBitmapCropped, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                    arrayImage = await EncodedBytes(displayableImage, BitmapEncoder.JpegEncoderId);
                    var nuevoStreamFace = new MemoryStream(arrayImage);



                   



                    //var ignored1 = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    //{
                    //    softwareBitmapSource.SetBitmapAsync(displayableImage);

                    //    imagenCamaraWeb.Source = softwareBitmapSource;

                    //});

                    string subscriptionKey = "a6fa05b6601b4ea398aa2039d601d983";
                    string subscriptionEndpoint = "https://southcentralus.api.cognitive.microsoft.com/face/v1.0";
                    var faceServiceClient = new FaceServiceClient(subscriptionKey, subscriptionEndpoint);

                    try
                    {


                        // using (var fsStream = File.OpenRead(sampleFile))
                        // {
                        IEnumerable<FaceAttributeType> faceAttributes =
                new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Emotion, FaceAttributeType.Glasses, FaceAttributeType.Hair };


                        var faces = await faceServiceClient.DetectAsync(nuevoStreamFace,true,false,faceAttributes);

                        string edad=string.Empty;
                        string genero = string.Empty;
                        var resultadoIdentifiacion = await faceServiceClient.IdentifyAsync(faces.Select(ff => ff.FaceId).ToArray(), largePersonGroupId: this.GroupId);

                        var ignored2 = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            try
                            {
                                var Status = faces.Length.ToString();
                                txtResultServicio.Text = "Caras encontradas: " + Status.ToString();


                            }
                            catch (Exception ex)
                            {
                                txtResultServicio.Text = "Error 1: " + ex.Message.ToString();

                                throw;
                            }
                            
                        });
                        

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
                                var nombrePersona = await faceServiceClient.GetPersonInLargePersonGroupAsync(GroupId, res.Candidates[0].PersonId);
                                PersonName = nombrePersona.Name.ToString();
                                //var estadoAnimo = 
                                var ignored3 = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    txtResult.Text = nombrePersona.Name.ToString() + " / " + genero.ToString();
                                });
                            }
                            else
                            {
                                txtResult.Text = "Unknown";
                            }
                        }
                        //}

                    }
                    catch (Exception ex)
                    {
                        var error = ex.Message.ToString();
                        var ignored3 = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            txtResultServicio.Text = "Error 2: "+ error;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                var mensaje = ex.Message.ToString();
                var ignored4 = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    txtResultServicio.Text = "Error 3: " + mensaje;
                });
            }
            return PersonName;

        }

        //public static IRandomAccessStream ConvertToRandomAccessStream(MemoryStream memoryStream)
        //{
        //    var randomAccessStream = new InMemoryRandomAccessStream();
        //    var outputStream = randomAccessStream.GetOutputStreamAt(0);
        //    var dw = new DataWriter(outputStream);
        //    var task = Task.Factory.StartNew(() => dw.WriteBytes(memoryStream.ToArray()));
        //     dw.StoreAsync();
        //     outputStream.FlushAsync();
        //    return randomAccessStream;
        //}
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
        private async Task SetupVisualization(Windows.Foundation.Size framePizelSize, ImageStream StreamImage, string Genero, string Edad, List<Tuple<string,string, FaceRectangle>> ListaCaraS)
        {


            this.VisualizationCanvas.Children.Clear();


            Image imagenCamaraWeb = new Image();
            imagenCamaraWeb.Width = 640;
            imagenCamaraWeb.Height = 360;
            imagenCamaraWeb.HorizontalAlignment = HorizontalAlignment.Center;
            imagenCamaraWeb.VerticalAlignment = VerticalAlignment.Center;
            imagenCamaraWeb.Stretch = Stretch.Fill;

            BitmapImage imagen = new BitmapImage();
            var StreamImageNuevo = StreamImage.AsStream();

            await imagen.SetSourceAsync(StreamImageNuevo.AsRandomAccessStream());
            imagenCamaraWeb.Source = imagen;

            this.VisualizationCanvas.Children.Add(imagenCamaraWeb);

            foreach (var elemento in ListaCaraS)
            {

                double actualWidth = this.VisualizationCanvas.ActualWidth;
                double actualHeight = this.VisualizationCanvas.ActualHeight;

                if (elemento.Item3 != null && actualWidth != 0 && actualHeight != 0)
                {
                    double widthScale = framePizelSize.Width / actualWidth;
                    double heightScale = framePizelSize.Height / actualHeight;

                    int i = 0;
                    var face = elemento.Item3;
                    Rectangle box = new Rectangle();

                    box.Width = (int)face.Width / (int)widthScale;
                    box.Height = (int)(face.Height / heightScale);
                    box.Fill = this.fillBrush;
                    box.Stroke = this.lineBrush;
                    box.StrokeThickness = this.lineThickness;
                    box.Margin = new Thickness((uint)(face.Left / widthScale), (uint)(face.Top / heightScale), 0, 0);

                    this.VisualizationCanvas.Children.Add(box);

                    TextBlock texto = new TextBlock();
                    texto.Text = Genero + " / " + Edad;
                    texto.Margin = new Thickness((uint)(face.Left / widthScale), (uint)(face.Top / heightScale) - 15, 0, 0);
                    texto.Foreground = this.lineBrush;
                    this.VisualizationCanvas.Children.Add(texto);


                }


            }
            
            listaCaras.Clear();







        }


        private async void btnTomarFoto_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.currentState = ScenarioState.Idle;
            ShutdownWebCam();
            this.Frame.Navigate(typeof(MainPage));
        }

        public ObservableCollection<Face> TargetFaces
        {
            get
            {
                return _faces;
            }
        }



        private async Task<SoftwareBitmap> CreateFromBitmap(SoftwareBitmap softwareBitmap, uint width, uint heigth)
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
        private async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
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

        public event PropertyChangedEventHandler PropertyChanged;


        public ObservableCollection<Person> Persons
        {
            get
            {
                return _persons;
            }
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            bool success = true;

            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();

                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                picker.FileTypeFilter.Add(".mp4");
                pickedFile = await picker.PickSingleFileAsync();
                if (pickedFile == null)
                {
                    return;
                }
                clip = await MediaClip.CreateFromFileAsync(pickedFile);

                //if (!frameProcessingSemaphore.Wait(0))
                //{
                //    return;
                //}
                //else
                //{
                    TimeSpan timerInterval = TimeSpan.FromMilliseconds(100);
                    this.frameProcessingTimer = Windows.System.Threading.ThreadPoolTimer.CreatePeriodicTimer(new Windows.System.Threading.TimerElapsedHandler(iniciarProceso), timerInterval);

                //}



            }
            catch (System.UnauthorizedAccessException)
            {
                // If the user has disabled their webcam this exception is thrown; provide a descriptive message to inform the user of this fact.
                //this.rootPage.NotifyUser("Webcam is disabled or access to the webcam is disabled for this app.\nEnsure Privacy Settings allow webcam usage.", NotifyType.ErrorMessage);
                success = false;
            }
            catch (Exception ex)
            {
                //this.rootPage.NotifyUser(ex.ToString(), NotifyType.ErrorMessage);
                success = false;
            }



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
            frameProcessingSemaphore.Release();

        }
    }
}
