using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accord.IO;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Statistics.Kernels;

namespace captchaengine
{
    public class MachineLearning
    {
        private readonly ImageProcessing _imageProcessing = new ImageProcessing();

        public Tuple<Bitmap[], string[]> LoadData(string dir)
        {
            var files = Directory.GetFiles(dir).Where(file => !string.IsNullOrEmpty(file) && (file.EndsWith("png") || file.EndsWith("jpeg") || file.EndsWith("jpg"))).ToArray();
            var data = new Bitmap[files.Length];
            var label = new string[files.Length];

            Parallel.For(0, files.Length, i =>
            {
                data[i] = (Bitmap)Image.FromFile(files[i]);
                label[i] = Path.GetFileName(files[i]).Split('-')[0];
            });

            return Tuple.Create(data, label);
        }

        public void Learn(Bitmap[] data, string[] label, string tribunal = null)
        {
            var count = label.Length;
            var charactersNumber = label[0].Length;
            
            var dataList = new double[count*charactersNumber][];
            var labelList = new int[count*charactersNumber];

            var mapLabel = BuildLabelIntMap(label);

            Parallel.For(0, count, i =>
            {
                var tempData = _imageProcessing.ExtractCharacters(data[i], tribunal);
                var tempLabel = LabelToInt(mapLabel, label[i]);

                for (var j = 0; j < charactersNumber; j++)
                {
                    dataList[charactersNumber * i + j] = tempData[j];
                    labelList[charactersNumber * i + j] = tempLabel[j];
                }
            });
            
            var engineDecide = new MulticlassSupportVectorLearning<Linear>
            {
                Learner = learner => new LinearDualCoordinateDescent<Linear>(),
                ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }
            }.Learn(dataList, labelList);

            engineDecide.Compress();
            Tuple.Create(engineDecide, mapLabel).Save(@".\" + tribunal + ".dat");
        }

        public string Decide(string filePath, string tribunal = null, string consulta = null, string color = null, bool learning = false)
        {
            var engineDecide = Serializer.Load<Tuple<MulticlassSupportVectorMachine<Linear>, char[]>>(@".\" + tribunal + ".dat");
            return IntToLabel(engineDecide.Item2, engineDecide.Item1.Decide(_imageProcessing.ExtractCharacters((Bitmap)Image.FromFile(filePath), tribunal, color).ToArray()));
        }

        public double Score(Bitmap[] data, string[] label, string tribunal = null, string consulta = null)
        {
            var count = label.Length;
            var charactersNumber = label[0].Length;

            var engineDecide = Serializer.Load<Tuple<MulticlassSupportVectorMachine<Linear>, char[]>>(@".\" + tribunal + ".dat");

            var dataList = new double[count * charactersNumber][];
            var labelList = new int[count * charactersNumber];

            Parallel.For(0, count, i =>
            {
                var tempData = _imageProcessing.ExtractCharacters(data[i], tribunal, consulta);
                var tempLabel = LabelToInt(engineDecide.Item2, label[i]);

                for (var j = 0; j < charactersNumber; j++)
                {
                    dataList[charactersNumber * i + j] = tempData[j];
                    labelList[charactersNumber * i + j] = tempLabel[j];
                }
            });

            var results = engineDecide.Item1.Decide(dataList);
            
            var sum = 0;
            for (var i = 0; i < count*charactersNumber; i++)
            {
                if (results[i] == labelList[i])
                {
                    sum++;
                }
            }
            
            return sum/(double)(count*charactersNumber);
        }

        private static char[] BuildLabelIntMap(string[] label) => string.Join("", label).ToCharArray().Distinct(l => l);

        private static int[] LabelToInt(char[] mapLabel, string label) => label.ToCharArray().Select(l => Array.IndexOf(mapLabel, l)).ToArray();

        private static string IntToLabel(char[] mapLabel, int[] intLabel) => intLabel.Aggregate("", (current, i) => current + mapLabel[i]);
    }
}