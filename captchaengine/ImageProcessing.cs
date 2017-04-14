using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Accord.Imaging.Converters;
using Accord.Imaging.Filters;
using Accord.IO;
using Accord.MachineLearning.VectorMachines;
using Accord.Math;
using Accord.Statistics.Kernels;

namespace captchaengine
{
    internal class ImageProcessing
    {
        public List<double[]> ExtractCharacters(Bitmap captchaImage, string tribunal = null, string color = null)
        {
            var methodToCall = this.GetType().GetMethod(tribunal.ToUpper());
            return methodToCall.GetParameters().Length > 1
                ? (List<double[]>) methodToCall.Invoke(this, new object[] {captchaImage, color})
                : (List<double[]>) methodToCall.Invoke(this, new object[] {captchaImage});
        }

        public List<double[]> TRT_PJE(Bitmap captchaImage)
        {
            var gs = new Grayscale(0, 0, 0);
            captchaImage = gs.Apply(captchaImage);
            var imageToMatrix = new ImageToMatrix();
            var delimiters = new[] {13, 29, 45, 61, 77, 93};
            captchaImage = new Bitmap(captchaImage, new Size(120, 35));
            var characters = new List<double[]>();

            foreach (var delimiter in delimiters)
            {
                byte[,] tmpBytes;
                imageToMatrix.Convert(captchaImage.Clone(new Rectangle { X = delimiter, Y = 10, Width = 16, Height = 23 }, captchaImage.PixelFormat), out tmpBytes);
                for (var i = 0; i < tmpBytes.GetLength(0); i++)
                {
                    for (var j = 0; j < tmpBytes.GetLength(1); j++)
                    {
                        if (tmpBytes[i, j] > 150)
                        {
                            tmpBytes[i, j] = 255;
                        }
                        else
                        {
                            tmpBytes[i, j] = 0;
                        }
                    }
                }
                characters.Add(tmpBytes.Flatten().ToDouble());
            }
            
            return characters;
        }

        public List<double[]> TJ_ESAJ_COLOR(Bitmap captchaImage, string color=null)
        {
            var imageToMatrix = new ImageToMatrix();
            var delimiters = new[] { 24, 51, 78, 105, 132, 159, 186, 213 };
            captchaImage = new Bitmap(captchaImage, new Size(260, 51));
            var characters = new List<double[]>();
            
            if (string.IsNullOrEmpty(color))
            {
                foreach (var delimiter in delimiters)
                {
                    byte[,] tempBytes;
                    Color[,] tempColors;
                    imageToMatrix.Convert(captchaImage.Clone(new Rectangle { X = delimiter, Y = 8, Width = 27, Height = 29 }, captchaImage.PixelFormat), out tempBytes);
                    imageToMatrix.Convert(captchaImage.Clone(new Rectangle { X = delimiter, Y = 8, Width = 27, Height = 29 }, captchaImage.PixelFormat), out tempColors);
                    var tempColor = tempColors.Cast<Color>().GroupBy(c => c.Name).OrderByDescending(cGroup => cGroup.Count()).Select(cGroup => cGroup.ElementAt(0)).ElementAt(1);

                    for (var i = 0; i < tempBytes.GetLength(0); i++)
                    {
                        for (var j = 0; j < tempBytes.GetLength(1); j++)
                        {
                            if (tempColors[i,j].Name == tempColor.Name)
                            {
                                tempBytes[i, j] = 0;
                            }
                            else
                            {
                                tempBytes[i, j] = 255;
                            }
                        }
                    }
                    characters.Add(tempBytes.Flatten().ToDouble());
                }
            }
            else
            {
                var engineColor = Serializer.Load<MulticlassSupportVectorMachine<Linear>>(@".\color.dat");
                
                foreach (var delimiter in delimiters)
                {
                    Color[,] tempColors;
                    imageToMatrix.Convert(captchaImage.Clone(new Rectangle { X = delimiter, Y = 8, Width = 27, Height = 29 }, captchaImage.PixelFormat), out tempColors);
                    var tempColor = tempColors.Cast<Color>().GroupBy(c => c.Name).OrderByDescending(cGroup => cGroup.Count()).Select(cGroup => cGroup.ElementAt(0)).ElementAt(1);

                    var intColor = engineColor.Decide(new double[]{tempColor.R, tempColor.G, tempColor.B});
                    var stringColor = "";

                    switch (intColor)
                    {
                        case 0:
                            stringColor = "rosa";
                            break;
                        case 1:
                            stringColor = "verde";
                            break;
                        case 2:
                            stringColor = "roxo";
                            break;
                        case 3:
                            stringColor = "laranja";
                            break;
                        case 4:
                            stringColor = "vermelho";
                            break;
                        case 5:
                            stringColor = "azul";
                            break;
                        case 6:
                            stringColor = "preto";
                            break;
                     }

                    if (stringColor == color.ToLower())
                    {
                        byte[,] tempBytes;
                        imageToMatrix.Convert(captchaImage.Clone(new Rectangle { X = delimiter, Y = 8, Width = 27, Height = 29 }, captchaImage.PixelFormat), out tempBytes);
                        imageToMatrix.Convert(captchaImage.Clone(new Rectangle { X = delimiter, Y = 8, Width = 27, Height = 29 }, captchaImage.PixelFormat), out tempColors);
                        tempColor = (tempColors.Cast<Color>().GroupBy(c => c.Name).OrderByDescending(cGroup => cGroup.Count()).Select(cGroup => cGroup.ElementAt(0))).ElementAt(1);

                        for (var i = 0; i < tempBytes.GetLength(0); i++)
                        {
                            for (var j = 0; j < tempBytes.GetLength(1); j++)
                            {
                                if (tempColors[i, j].Name == tempColor.Name)
                                {
                                    tempBytes[i, j] = 0;
                                }
                                else
                                {
                                    tempBytes[i, j] = 255;
                                }
                            }
                        }
                        characters.Add(tempBytes.Flatten().ToDouble());
                    }
                }
            }

            return characters;
        }

        public List<double[]> TJ_PJE(Bitmap captchaImage)
        {
            var gs = new Grayscale(0, 0, 0);
            captchaImage = gs.Apply(captchaImage);
            var imageToMatrix = new ImageToMatrix();
            var delimiters = new[] { 12, 28, 44, 60, 76, 92 };
            captchaImage = new Bitmap(captchaImage, new Size(120, 40));
            var characters = new List<double[]>();

            foreach (var delimiter in delimiters)
            {
                byte[,] tmpBytes;
                imageToMatrix.Convert(captchaImage.Clone(new Rectangle { X = delimiter, Y = 11, Width = 16, Height = 24 }, captchaImage.PixelFormat), out tmpBytes);
                for (var i = 0; i < tmpBytes.GetLength(0); i++)
                {
                    for (var j = 0; j < tmpBytes.GetLength(1); j++)
                    {
                        if (tmpBytes[i, j] > 190)
                        {
                            tmpBytes[i, j] = 255;
                        }
                        else
                        {
                            tmpBytes[i, j] = 0;
                        }
                    }
                }
                characters.Add(tmpBytes.Flatten().ToDouble());
            }

            return characters;
        }

        public void ExtractFeatures()
        {
            
        }
    }
}
