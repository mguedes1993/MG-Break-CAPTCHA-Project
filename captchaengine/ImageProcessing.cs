using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Accord.Imaging.Converters;
using Accord.Imaging.Filters;
using Accord.IO;
using Accord.MachineLearning.VectorMachines;
using Accord.Math;
using Accord.Statistics.Kernels;

namespace CaptchaEngine
{
    internal class ImageProcessing
    {
        public double[][] ExtractCharacters(Bitmap captchaImage, string court, string color = null)
        {
            var methodToCall = GetType().GetMethod(court.ToUpper(), BindingFlags.NonPublic | BindingFlags.Instance);
            return methodToCall.GetParameters().Length > 1
                ? (double[][]) methodToCall.Invoke(this, new object[] {captchaImage, color})
                : (double[][]) methodToCall.Invoke(this, new object[] {captchaImage});
        }

        private double[][] TRT_PJE(Bitmap captchaImage)
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
                imageToMatrix.Convert(captchaImage.Clone(new Rectangle {X = delimiter, Y = 10, Width = 16, Height = 23}, captchaImage.PixelFormat), out tmpBytes);
                for (var i = 0; i < tmpBytes.GetLength(0); i++)
                {
                    for (var j = 0; j < tmpBytes.GetLength(1); j++)
                    {
                        if (tmpBytes[i, j] > 150)
                        {
                            tmpBytes[i, j] = 1;
                        }
                        else
                        {
                            tmpBytes[i, j] = 0;
                        }
                    }
                }
                characters.Add(tmpBytes.Flatten().ToDouble());
            }

            return characters.ToArray();
        }

        private double[][] TJ_ESAJ_COLOR(Bitmap captchaImage, string color)
        {
            var imageToMatrix = new ImageToMatrix();
            var delimiters = new[] {24, 51, 78, 105, 132, 159, 186, 213};
            captchaImage = new Bitmap(captchaImage, new Size(260, 51));
            var characters = new List<double[]>();

            if (string.IsNullOrEmpty(color))
            {
                foreach (var delimiter in delimiters)
                {
                    byte[,] tempBytes;
                    Color[,] tempColors;
                    imageToMatrix.Convert(captchaImage.Clone(new Rectangle {X = delimiter, Y = 8, Width = 27, Height = 29}, captchaImage.PixelFormat), out tempBytes);
                    imageToMatrix.Convert(captchaImage.Clone(new Rectangle {X = delimiter, Y = 8, Width = 27, Height = 29}, captchaImage.PixelFormat), out tempColors);
                    var tempColor = tempColors.Cast<Color>().GroupBy(c => c.Name).OrderByDescending(cGroup => cGroup.Count()).Select(cGroup => cGroup.ElementAt(0)).ElementAt(1);

                    for (var i = 0; i < tempBytes.GetLength(0); i++)
                    {
                        for (var j = 0; j < tempBytes.GetLength(1); j++)
                        {
                            if (tempColors[i, j].Name == tempColor.Name)
                            {
                                tempBytes[i, j] = 1;
                            }
                            else
                            {
                                tempBytes[i, j] = 0;
                            }
                        }
                    }

                    characters.Add(tempBytes.Flatten().ToDouble());
                }
            }
            else
            {
                var engineColor = Serializer.Load<MulticlassSupportVectorMachine<Linear>>(Assembly.GetExecutingAssembly().GetManifestResourceStream("captchaengine.color.dat"));

                foreach (var delimiter in delimiters)
                {
                    Color[,] tempColors;
                    imageToMatrix.Convert(captchaImage.Clone(new Rectangle {X = delimiter, Y = 8, Width = 27, Height = 29}, captchaImage.PixelFormat), out tempColors);
                    var tempColor = tempColors.Cast<Color>().GroupBy(c => c.Name).OrderByDescending(cGroup => cGroup.Count()).Select(cGroup => cGroup.ElementAt(0)).ElementAt(1);

                    var intColor = engineColor.Decide(new double[] {tempColor.R, tempColor.G, tempColor.B});
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
                        imageToMatrix.Convert(captchaImage.Clone(new Rectangle {X = delimiter, Y = 8, Width = 27, Height = 29}, captchaImage.PixelFormat), out tempBytes);
                        imageToMatrix.Convert(captchaImage.Clone(new Rectangle {X = delimiter, Y = 8, Width = 27, Height = 29}, captchaImage.PixelFormat), out tempColors);
                        tempColor = tempColors.Cast<Color>().GroupBy(c => c.Name).OrderByDescending(cGroup => cGroup.Count()).Select(cGroup => cGroup.ElementAt(0)).ElementAt(1);

                        for (var i = 0; i < tempBytes.GetLength(0); i++)
                        {
                            for (var j = 0; j < tempBytes.GetLength(1); j++)
                            {
                                if (tempColors[i, j].Name == tempColor.Name)
                                {
                                    tempBytes[i, j] = 1;
                                }
                                else
                                {
                                    tempBytes[i, j] = 0;
                                }
                            }
                        }

                        characters.Add(tempBytes.Flatten().ToDouble());
                    }
                }
            }

            return characters.ToArray();
        }

        private double[][] TJ_PJE(Bitmap captchaImage)
        {
            var gs = new Grayscale(0, 0, 0);
            var imageToMatrix = new ImageToMatrix();

            captchaImage = gs.Apply(captchaImage);
            var delimiters = new[] {12, 28, 44, 60, 76, 92};
            captchaImage = new Bitmap(captchaImage, new Size(120, 40));
            var characters = new List<double[]>();

            foreach (var delimiter in delimiters)
            {
                byte[,] tmpBytes;
                imageToMatrix.Convert(captchaImage.Clone(new Rectangle {X = delimiter, Y = 11, Width = 16, Height = 24}, captchaImage.PixelFormat), out tmpBytes);
                for (var i = 0; i < tmpBytes.GetLength(0); i++)
                {
                    for (var j = 0; j < tmpBytes.GetLength(1); j++)
                    {
                        if (tmpBytes[i, j] > 190)
                        {
                            tmpBytes[i, j] = 1;
                        }
                        else
                        {
                            tmpBytes[i, j] = 0;
                        }
                    }
                }

                characters.Add(tmpBytes.Flatten().ToDouble());
            }

            return characters.ToArray();
        }

        private double[][] TJRS_FISICO(Bitmap captchaImage)
        {
            var imageToMatrix = new ImageToMatrix();
            var delimiters = new[] {10, 35, 60, 85};
            captchaImage = new Bitmap(captchaImage, new Size(120, 40));
            var characters = new List<double[]>();

            foreach (var delimiter in delimiters)
            {
                var characterImg = captchaImage.Clone(new Rectangle(delimiter, 3, 25, 34), captchaImage.PixelFormat);
                Color[,] imgColors;
                double[,] imgBytes;
                imageToMatrix.Convert(characterImg, out imgColors);
                imageToMatrix.Convert(characterImg, out imgBytes);

                for (var i = 3; i < imgBytes.GetLength(0) - 3; i++)
                {
                    for (var j = 3; j < imgBytes.GetLength(1) - 3; j++)
                    {
                        var r = imgColors[i, j].R;
                        var g = imgColors[i, j].G;
                        var b = imgColors[i, j].B;

                        var toleranceIndexes = new[]
                        {
                            imgColors[i - 1, j - 1],
                            imgColors[i - 1, j],
                            imgColors[i - 1, j + 1],

                            imgColors[i, j - 1],
                            imgColors[i, j + 1],

                            imgColors[i + 1, j - 1],
                            imgColors[i + 1, j],
                            imgColors[i + 1, j + 1]
                        };

                        var toleranceIndexes2 = new[]
                        {
                            imgColors[i - 3, j - 3],
                            imgColors[i - 3, j - 2],
                            imgColors[i - 3, j - 1],
                            imgColors[i - 3, j],
                            imgColors[i - 3, j + 1],
                            imgColors[i - 3, j + 2],
                            imgColors[i - 3, j + 3],

                            imgColors[i - 2, j - 3],
                            imgColors[i - 2, j - 2],
                            imgColors[i - 2, j - 1],
                            imgColors[i - 2, j],
                            imgColors[i - 2, j + 1],
                            imgColors[i - 2, j + 2],
                            imgColors[i - 2, j + 3],

                            imgColors[i - 1, j - 3],
                            imgColors[i - 1, j - 2],
                            imgColors[i - 1, j - 1],
                            imgColors[i - 1, j],
                            imgColors[i - 1, j + 1],
                            imgColors[i - 1, j + 2],
                            imgColors[i - 1, j + 3],

                            imgColors[i, j - 3],
                            imgColors[i, j - 2],
                            imgColors[i, j - 1],
                            imgColors[i, j + 1],
                            imgColors[i, j + 2],
                            imgColors[i, j + 3],

                            imgColors[i + 1, j - 3],
                            imgColors[i + 1, j - 2],
                            imgColors[i + 1, j - 1],
                            imgColors[i + 1, j],
                            imgColors[i + 1, j + 1],
                            imgColors[i + 1, j + 2],
                            imgColors[i + 1, j + 3],

                            imgColors[i + 2, j - 3],
                            imgColors[i + 2, j - 2],
                            imgColors[i + 2, j - 1],
                            imgColors[i + 2, j],
                            imgColors[i + 2, j + 1],
                            imgColors[i + 2, j + 2],
                            imgColors[i + 2, j + 3],

                            imgColors[i + 3, j - 3],
                            imgColors[i + 3, j - 2],
                            imgColors[i + 3, j - 1],
                            imgColors[i + 3, j],
                            imgColors[i + 3, j + 1],
                            imgColors[i + 3, j + 2],
                            imgColors[i + 3, j + 3]
                        };

                        //var ver = false;
                        var cont = 0;

                        foreach (var color in toleranceIndexes)
                        {
                            var tolerance = 25;
                            if (color.R <= r + tolerance && color.R >= r - tolerance && color.G <= g + tolerance &&
                                color.G >= g - tolerance && color.B <= b + tolerance && color.B >= b - tolerance)
                            {
                                cont++;
                            }
                        }

                        imgBytes[i, j] = Math.Pow(2, -(8 - cont));

                        //if (cont >= 3)
                        //{
                        //    ver = true;
                        //}

                        //if (ver)
                        //{
                        //    imgBytes[i, j] = 1;
                        //}
                        //else
                        //{
                        //    imgBytes[i, j] = 0;
                        //}
                    }
                }

                characters.Add(imgBytes.Flatten());
            }

            return characters.ToArray();
        }

        private void ExtractFeatures()
        {

        }
    }
}