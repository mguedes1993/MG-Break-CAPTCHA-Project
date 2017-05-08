using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accord.IO;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Accord.Statistics.Kernels;

namespace CaptchaEngine
{
    public class CaptchaDecoder
    {
        private readonly ImageProcessing _imageProcessing = new ImageProcessing();

        #region Machine Learning

        public void MachineLearningTraining(double[][][] data, string[] label, string court)
        {
            var countData = data.Length;

            var newData = new List<double[]>();
            var newLabel = new List<int>();

            var mapLabel = BuildLabelIntMap(label);

            for (var i = 0; i < countData; i++)
            {
                var tmpData = data[i];
                var tmpLabel = LabelToInt(mapLabel, label[i]);

                for (var j = 0; j < tmpData.Length; j++)
                {
                    newData.Add(tmpData[j]);
                    newLabel.Add(tmpLabel[j]);
                }
            }

            var engine = new MulticlassSupportVectorLearning<Linear>
            {
                Learner = learner => new LinearDualCoordinateDescent<Linear>(),
                ParallelOptions = new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount}
            }.Learn(newData.ToArray(), newLabel.ToArray());
            engine.Compress();
            Tuple.Create(engine, mapLabel, newData[0].Length).Save(@".\MLengine_" + court + ".dat");
        }

        public string MachineLearningPredict(string filePath, string court, string color = null)
        {
            var engine =
                Serializer.Load<Tuple<MulticlassSupportVectorMachine<Linear>, char[], int>>(
                    @".\MLengine_" + court + ".dat");
            var tmpImage = (Bitmap) Image.FromFile(filePath);
            var characters = _imageProcessing.ExtractCharacters(tmpImage, court, color);
            tmpImage.Dispose();
            for (var i = 0; i < characters.Length; i++)
            {
                Array.Resize(ref characters[i], engine.Item3);
            }
            return IntToLabel(engine.Item2, engine.Item1.Decide(characters));
        }

        public double MachineLearningScore(double[][][] data, string[] label, string court)
        {
            var countData = label.Length;

            var engine =
                Serializer.Load<Tuple<MulticlassSupportVectorMachine<Linear>, char[], int>>(
                    @".\MLengine_" + court + ".dat");

            var newData = new List<double[]>();
            var newLabel = new List<int>();

            for (var i = 0; i < countData; i++)
            {
                var tmpData = data[i];
                var tmpLabel = LabelToInt(engine.Item2, label[i]);

                for (var j = 0; j < tmpData.Length; j++)
                {
                    Array.Resize(ref tmpData[j], engine.Item3);
                    newData.Add(tmpData[j]);
                    newLabel.Add(tmpLabel[j]);
                }
            }

            return 1 - new ZeroOneLoss(newLabel.ToArray()).Loss(engine.Item1.Decide(newData.ToArray()));
        }

        #endregion

        #region Neural

        public void NeuralTraining(double[][][] data, string[] label)
        {
            var countData = label.Length;

            var newData = new List<double[]>();
            var newLabel = new List<double[]>();

            var mapLabel = BuildLabelIntMap(label);
            var mapNeuralLabel = BuildNeuralLabelDoublesMap(mapLabel);

            for (var i = 0; i < countData; i++)
            {
                var tmpData = data[i];
                var tmpLabel = NeuralLabelToDoubles(mapLabel, mapNeuralLabel, label[i]);

                for (var j = 0; j < tmpLabel.Length; j++)
                {
                    newData.Add(tmpData[j]);
                    newLabel.Add(tmpLabel[j]);
                }
            }

            var nInputs = newData[0].Length;
            var nOutputs = mapLabel.Length;

            var layers = new[] {nOutputs, nOutputs};

            var neuralNetwork = new DeepBeliefNetwork(nInputs, layers);
            new GaussianWeights(neuralNetwork).Randomize();
            neuralNetwork.UpdateVisibleWeights();

            var neuralLearningSupervised = new BackPropagationLearning(neuralNetwork); //Supervised Learning

            var score = new List<double>();
            var count = 0;
            var stop = 0;

            while (stop < 5)
            {
                neuralLearningSupervised.RunEpoch(newData.ToArray(), newLabel.ToArray());

                if (count % 5 == 0)
                {
                    Tuple.Create(neuralNetwork, mapLabel, nInputs).Save(@".\NeuralEngine.dat");
                    score.Add(NeuralScore(data, label));
                }

                if (score.Last() > 0.90 || stop == 4)
                {
                    var errorData = NeuralErrorData(neuralNetwork, newData.ToArray(), newLabel.ToArray());

                    if (errorData.Item1.Length == 0) break;

                    for (var i = 0; i < count * 5; i++)
                    {
                        neuralLearningSupervised.RunEpoch(errorData.Item1, errorData.Item2);
                    }
                }

                if (score.Count >= 50 && count % 5 == 0)
                {
                    var nChanges = score.GetRange(score.Count - 3, 3).ToArray().Distinct(t => t).Length;

                    if (nChanges == 1)
                    {
                        stop++;
                    }
                    else
                    {
                        stop = 0;
                    }
                }

                count++;
            }

            Tuple.Create(neuralNetwork, mapLabel, nInputs).Save(@".\NeuralEngine.dat");
        }

        public string NeuralPredict(string filePath, string court, string color = null)
        {
            var engine = Serializer.Load<Tuple<DeepBeliefNetwork, char[], int>>(@".\NeuralEngine.dat");
            var tmpImage = (Bitmap) Image.FromFile(filePath);
            var characters = _imageProcessing.ExtractCharacters(tmpImage, court, color);
            tmpImage.Dispose();
            for (var i = 0; i < characters.Length; i++)
            {
                Array.Resize(ref characters[i], engine.Item3);
            }
            return characters.Aggregate("",
                (current, i) => current + NeuralDoublesToLabel(engine.Item2, engine.Item1.Compute(i)));
        }

        public double NeuralScore(double[][][] data, string[] label)
        {
            var countData = label.Length;

            var neuralNetwork = Serializer.Load<Tuple<DeepBeliefNetwork, char[], int>>(@".\NeuralEngine.dat");

            var newData = new List<double[]>();
            var newLabel = new List<int>();

            for (var i = 0; i < countData; i++)
            {
                var tmpData = data[i];
                var tmpLabel = LabelToInt(neuralNetwork.Item2, label[i]);

                for (var j = 0; j < tmpData.Length; j++)
                {
                    Array.Resize(ref tmpData[j], neuralNetwork.Item3);
                    newData.Add(tmpData[j]);
                    newLabel.Add(tmpLabel[j]);
                }
            }

            var results = new List<int>();

            foreach (var i in newData)
            {
                var tmpResult = neuralNetwork.Item1.Compute(i);
                results.Add(Array.IndexOf(tmpResult, tmpResult.Max()));
            }

            return 1 - new ZeroOneLoss(newLabel.ToArray()).Loss(results.ToArray());
        }

        private static Tuple<double[][], double[][]> NeuralErrorData(Network neuralNetwork, double[][] data, double[][] label)
        {
            var errorData = new List<double[]>();
            var errorLabel = new List<double[]>();
            for (var i = 0; i < data.Length; i++)
            {
                var resultDoubles = neuralNetwork.Compute(data[i]);

                if (Array.IndexOf(resultDoubles, resultDoubles.Max()) != Array.IndexOf(label[i], label[i].Max()))
                {
                    errorData.Add(data[i]);
                    errorLabel.Add(label[i]);
                }
            }

            return Tuple.Create(errorData.ToArray(), errorLabel.ToArray());
        }

        #endregion

        #region Tools

        public Tuple<double[][][], string[], string> LoadDataset(string dir, string court)
        {
            var files = Directory.GetFiles(dir)
                .Where(file => !string.IsNullOrEmpty(file) &&
                               (file.EndsWith("png") || file.EndsWith("jpeg") || file.EndsWith("jpg"))).ToArray();
            var data = new double[files.Length][][];
            var label = new string[files.Length];

            Parallel.For(0, files.Length, i =>
            {
                var tmpImage = (Bitmap) Image.FromFile(files[i]);
                data[i] = _imageProcessing.ExtractCharacters(tmpImage, court);
                tmpImage.Dispose();
                label[i] = Path.GetFileName(files[i]).Split('-')[0];
            });

            return Tuple.Create(data, label, court);
        }

        public static double[][][] NormalizeDataset(double[][][] data)
        {
            //[samples] [number of characters] [data]
            var maxSize = (from i in data from j in i select j.Length).Max();

            for (var j = 0; j < data.Length; j++) //Sample
            {
                for (var k = 0; k < data[j].Length; k++) //Characters
                {
                    if (data[j][k].Length != maxSize)
                    {
                        Array.Resize(ref data[j][k], maxSize);
                    }
                }
            }

            return data;
        }

        private static double[][] BuildNeuralLabelDoublesMap(char[] mapLabel)
        {
            var mapLabelNeural = new List<double[]>();

            for (var i = 0; i < mapLabel.Length; i++)
            {
                var tmpDoubles = new double[mapLabel.Length];
                for (var j = 0; j < mapLabel.Length; j++)
                {
                    if (i == j)
                    {
                        tmpDoubles[j] = 1;
                    }
                    else
                    {
                        tmpDoubles[j] = 0;
                    }
                }
                mapLabelNeural.Add(tmpDoubles);
            }

            return mapLabelNeural.ToArray();
        }

        private static double[][] NeuralLabelToDoubles(char[] mapLabel, double[][] mapLabelNeural, string label) =>
            LabelToInt(mapLabel, label).Select(li => mapLabelNeural[li]).ToArray();

        private static string NeuralDoublesToLabel(char[] mapLabel, double[] label) => IntToLabel(mapLabel,
            new[] {Array.IndexOf(label, label.Max())});

        private static char[] BuildLabelIntMap(string[] label) => string.Join("", label).ToCharArray().Distinct(l => l);

        private static int[] LabelToInt(char[] mapLabel, string label) => label.ToCharArray()
            .Select(l => Array.IndexOf(mapLabel, l)).ToArray();

        private static string IntToLabel(char[] mapLabel, int[] intLabel) => intLabel.Aggregate("",
            (current, i) => current + mapLabel[i]);

        #endregion
    }
}