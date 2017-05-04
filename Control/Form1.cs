using System;
using System.Windows.Forms;
using CaptchaEngine;

namespace Control
{
    public partial class Form1 : Form
    {
        private readonly CaptchaDecoder _ce = new CaptchaDecoder();
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonLearn_Click(object sender, EventArgs e)
        {
            var path = @"..\..\..\datasets\" + comboBox1.SelectedItem.ToString();
            var data = _ce.LoadDataset(path, comboBox1.SelectedItem.ToString());
            _ce.NeuralTraining(data.Item1, data.Item2);
        }

        private void buttonDecide_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = @"Images | *.jpeg;*.jpg;*.png";
            openFileDialog1.ShowDialog();
            pictureBox1.ImageLocation = openFileDialog1.FileName;
            var result = _ce.NeuralPredict(openFileDialog1.FileName, comboBox1.SelectedItem.ToString(), textBoxColor.Text);
            textBoxResult.Text = result;
        }

        private void buttonScore_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            if (folderBrowserDialog1.SelectedPath != "")
            {
                var data = _ce.LoadDataset(folderBrowserDialog1.SelectedPath, comboBox1.SelectedItem.ToString());
                var score = _ce.NeuralScore(data.Item1, data.Item2);
                textBoxResult.Text = score.ToString("#.0000%");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var data_TJ_PJE = _ce.LoadDataset(@"..\..\..\datasets\tj_pje", "tj_pje");
            var data_TRT_PJE = _ce.LoadDataset(@"..\..\..\datasets\trt_pje", "trt_pje");
            var data_TJ_ESAJ_COLOR = _ce.LoadDataset(@"..\..\..\datasets\tj_esaj_color", "tj_esaj_color");
            
            var allData = new double[data_TJ_PJE.Item1.Length + data_TRT_PJE.Item1.Length + data_TJ_ESAJ_COLOR.Item1.Length][][];
            Array.Copy(data_TJ_PJE.Item1, allData, data_TJ_PJE.Item1.Length);
            Array.Copy(data_TRT_PJE.Item1, 0, allData, data_TJ_PJE.Item1.Length, data_TRT_PJE.Item1.Length);
            Array.Copy(data_TJ_ESAJ_COLOR.Item1, 0, allData, data_TJ_PJE.Item1.Length + data_TRT_PJE.Item1.Length, data_TJ_ESAJ_COLOR.Item1.Length);

            var allLabel = new string[data_TJ_PJE.Item2.Length + data_TRT_PJE.Item2.Length + data_TJ_ESAJ_COLOR.Item2.Length];
            Array.Copy(data_TJ_PJE.Item2, allLabel, data_TJ_PJE.Item2.Length);
            Array.Copy(data_TRT_PJE.Item2, 0, allLabel, data_TJ_PJE.Item2.Length, data_TRT_PJE.Item2.Length);
            Array.Copy(data_TJ_ESAJ_COLOR.Item2, 0, allLabel, data_TJ_PJE.Item2.Length + data_TRT_PJE.Item2.Length, data_TJ_ESAJ_COLOR.Item2.Length);

            var normalizedData = CaptchaDecoder.NormalizeDataset(allData);
            
            _ce.NeuralTraining(normalizedData, allLabel);
        }
    }
}
