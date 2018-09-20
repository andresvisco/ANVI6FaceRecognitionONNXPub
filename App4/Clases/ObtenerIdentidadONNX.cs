using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Media;
using Windows.Storage;

namespace App5.Clases
{
    public class ObtenerIdentidadONNX
    {
        private int _runCount = 0;

        public string identidadEncontradaTexto = string.Empty;
        public async Task<string> ObtenerIdentidadOnnX(VideoFrame videoFrame, LearningModelSession _session)
        {
            identidadEncontradaTexto = string.Empty;
            if (videoFrame != null)
            {
                try
                {
                    LearningModelBinding binding = new LearningModelBinding(_session);
                    ImageFeatureValue imageTensor = ImageFeatureValue.CreateFromVideoFrame(videoFrame);
                    binding.Bind("data", imageTensor);
                    int ticks = Environment.TickCount;

                    // Process the frame with the model
                    var results = await _session.EvaluateAsync(binding, $"Run { ++_runCount } ");

                    ticks = Environment.TickCount - ticks;
                    var label = results.Outputs["classLabel"] as TensorString;
                    var resultVector = label.GetAsVectorView();
                    List<float> topProbabilities = new List<float>() { 0.0f, 0.0f, 0.0f };
                    List<int> topProbabilityLabelIndexes = new List<int>() { 0, 0, 0 };
                    // SqueezeNet returns a list of 1000 options, with probabilities for each, loop through all
                    for (int i = 0; i < resultVector.Count(); i++)
                    {
                        // is it one of the top 3?
                        for (int j = 0; j < 3; j++)
                        {
                            identidadEncontradaTexto = resultVector[i].ToString();

                            //if (resultVector[i] > topProbabilities[j])
                            //{
                            //    topProbabilityLabelIndexes[j] = i;
                            //    topProbabilities[j] = resultVector[i];
                            //    break;
                            //}
                        }
                    }

                }

                catch (Exception ex)
                {

                    identidadEncontradaTexto = "";
                }
            }
            return identidadEncontradaTexto;

        }

    }
}
