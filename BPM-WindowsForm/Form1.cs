using BayesPointMachine;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BayesPointMachineForm
{
    public partial class Form1 : Form
    {
        bool performingCalcs = false;
        private string resultsFilePath = @"";
        private string trainingFilePath = @"";
        private string testFilePath = @"";
        private int noOfFeatures = 0;
        private int noOfRuns = 0;
        private double startSensitivity = 0.1;
        private double maxSensitivity = 10.0;
        private double sensitivityIncrement = 0.1;
        private double noisePrecision = 1.0;
        private int numOfClasses = 2;
        private bool labelAtStartOfLine = false;
        private int totalRuns, runsLeft;
        private bool addBias = false;

        public Form1()
        {
            InitializeComponent();
            SetHandlers();
            progressBar1.Minimum = 0;
        }

        private void SetHandlers()
        {
            trainingFileSelect.Click += (this.trainingFileSelect_Click);
            testFileSelect.Click += (this.testFileSelect_Click);
            beginButton.Click += (this.begin_Click);
            resultsFileSelect.Click += (this.saveFileSelect_Click);
            checkBox1.Click += (this.checkbox1_Click);
            numericUpDown1.ValueChanged += (this.noOfFeatures_Changed);
            numericUpDown2.ValueChanged += (this.noOfRuns_Changed);
            textBox2.TextChanged += (this.startingSensitivity_Changed);
            textBox3.TextChanged += (this.maximumSensitivity_Changed);
            textBox4.TextChanged += (this.sensitivityIncrement_Changed);
        }

        private static double RunBPMGeneral(BPMDataModel model, int numClasses, double noisePrecision, bool addBias, Vector[] testSet, int noOfFeatures, int[] testResults)
        {
            double accuracy;
            int[] labels = model.getClasses();
            int correctCount = 0;
            Vector[] featureVectors = new Vector[noOfFeatures];
            BPM bpm = new BPM(numClasses, noisePrecision);
            VectorGaussian[] posteriorWeights = bpm.Train(model.GetInputs(), model.getClasses());
			//Console.WriteLine("Weights=" + StringUtil.ArrayToString(posteriorWeights));

            //Console.WriteLine("\nPredictions:");
            Discrete[] predictions = bpm.Test(testSet);
            int i = 0;
            foreach (Discrete prediction in predictions)
            {
                //Console.WriteLine(prediction);
                if (FindMaxValPosition(prediction.GetProbs().ToArray()) == testResults[i]) correctCount++;
                //if ((prediction.GetProbs().ToArray()[0] > 0.5) && (testResults[i] == 0) || ((prediction.GetProbs().ToArray()[1] > 0.5) && (testResults[i] == 1)))
                //{
                //    correctCount++;
                //}
                i++;
            }
            accuracy = ((double)correctCount / predictions.Length) * 100;
            //Console.WriteLine();
            double logEvidence = bpm.GetModelEvidence(featureVectors, labels);
            //Console.WriteLine("No of correct predictions: " + correctCount);
            //Console.WriteLine("Evidence for model:");
            //Console.WriteLine(logEvidence);
            //Console.WriteLine();
            return accuracy;
        }

        #region handlers

        private void trainingFileSelect_Click(object sender, System.EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                trainingFilePath = openFileDialog1.FileName;
            }
        }

        private void testFileSelect_Click(object sender, System.EventArgs e)
        {
            if(openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                testFilePath = openFileDialog2.FileName;
            }
        }

        private void saveFileSelect_Click(object sender, System.EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                resultsFilePath = saveFileDialog1.FileName;
            }
        }

        private void checkbox1_Click(object sender, System.EventArgs e)
        {
            labelAtStartOfLine = checkBox1.Checked;
        }

        private void noOfFeatures_Changed(object sender, System.EventArgs e)
        {
            noOfFeatures = (int)numericUpDown1.Value;
        }

        private void noOfRuns_Changed(object sender, System.EventArgs e)   
        {
            noOfRuns = (int)numericUpDown2.Value;
        }

        private void startingSensitivity_Changed(object sender, System.EventArgs e)
        {
            startSensitivity = Double.Parse(textBox2.Text);
        }

        private void maximumSensitivity_Changed(object sender, System.EventArgs e)
        {
            maxSensitivity = Double.Parse(textBox3.Text);
        }

        private void sensitivityIncrement_Changed(object sender, System.EventArgs e)
        {
            sensitivityIncrement = Double.Parse(textBox4.Text);
        }

        #endregion

        private void begin_Click(object sender, System.EventArgs e)
        {
            Thread calcThread = new Thread(RunTests);
            calcThread.Name = "CalcThread";
            calcThread.Priority = ThreadPriority.BelowNormal;
            totalRuns = (int)(noOfRuns * (1 + ((maxSensitivity - startSensitivity) / sensitivityIncrement)));
            progressBar1.Maximum = totalRuns;
            performingCalcs = true;
            calcThread.Start();
            while (performingCalcs)
            {
                Thread.Sleep(100);
                progressBar1.Value = (totalRuns - runsLeft);
            }
        }

        private void RunTests()
        {
            StreamWriter writer = new StreamWriter(resultsFilePath);
            //int noOfFeatures = 4;
            int noOfTestValues = 274;
            //int noOfTestValues = 15060;
            int noOfTrainingValues = 1098;
            //int noOfTrainingValues = 30162;
            //string trainingFilePath = @"..\..\data\banknotesdata.txt";
            //string trainingFilePath = @"..\..\data\adultTraining.txt";
            BPMDataModel trainingModel = FileUtils.Read(trainingFilePath, labelAtStartOfLine, noOfTrainingValues,
                noOfFeatures, addBias);
            BPMDataModel noisyModel = FileUtils.CreateNoisyModel(trainingModel, noisePrecision);

            //string testFile = @"..\..\data\banknotestraining.txt";
            //string testFile = @"..\..\data\adultTest.txt";s
            BPMDataModel testModel = FileUtils.Read(testFilePath, labelAtStartOfLine, noOfTestValues, noOfFeatures, addBias);
            Vector[] testVectors = testModel.GetInputs();
            int runsLeft = totalRuns;
            double accuracy = 0;
            for (double i = startSensitivity; i <= maxSensitivity; i = (i + sensitivityIncrement))
            {
                //writer.WriteLine(i);
                for (int j = 0; j < noOfRuns; j++)
                {
                    noisyModel = FileUtils.CreateNoisyModel(trainingModel, i);
                    accuracy = RunBPMGeneral(noisyModel, numOfClasses, noisePrecision, addBias, testVectors, noOfFeatures, testModel.getClasses());
                    totalRuns--;
                    writer.WriteLine(i + "," + accuracy);
                    //textBox1.Text = ("Done " + j + " of acc: " + i);
                }
                //progressBar1.Increment(1);
            }
            writer.Flush();
            writer.Close();
            performingCalcs = false;
        }

        private static int FindMaxValPosition(double[] values)
        {
            double maxValue = values.Max();
            int i =0;
            foreach (double val in values)
            {
                if (val == maxValue)
                    return i;
                i++;
            }
            return -1;
        }


    }
}
