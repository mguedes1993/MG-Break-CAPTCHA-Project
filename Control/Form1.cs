using System;
using System.Windows.Forms;
using captchaengine;

namespace Control
{
    public partial class Form1 : Form
    {
        private readonly MachineLearning _ce = new MachineLearning();
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonLearn_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            if (folderBrowserDialog1.SelectedPath != "")
            {
                var data = _ce.LoadData(folderBrowserDialog1.SelectedPath);
                _ce.Learn(data.Item1, data.Item2, tribunal: comboBox1.SelectedItem.ToString());
                data = null;
            }
        }

        private void buttonDecide_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Imagens | *.jpg;*.png";
            openFileDialog1.ShowDialog();
            pictureBox1.ImageLocation = openFileDialog1.FileName;
            var result = _ce.Decide(openFileDialog1.FileName, tribunal: comboBox1.SelectedItem.ToString(), color: textBoxColor.Text);
            textBoxResult.Text = result;
        }

        private void buttonScore_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            if (folderBrowserDialog1.SelectedPath != "")
            {
                var data = _ce.LoadData(folderBrowserDialog1.SelectedPath);
                var score = _ce.Score(data.Item1, data.Item2, tribunal: comboBox1.SelectedItem.ToString());
                textBoxResult.Text = (score*100).ToString();
                data = null;
            }
        }
    }
}
