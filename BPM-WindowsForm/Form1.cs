using BayesPointMachine;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace BayesPointMachineForm
{
    public partial class Form1 : Form
    {
        #region variables
        bool _performingCalcs;
        private string _resultsFilePath = @"";
        private string _trainingFilePath = @"";
        private string _testFilePath = @"";
        private int _noOfFeatures;
        private int _noOfRuns;
        private double _startSensitivity = 0.1;
        private double _maxSensitivity = 10.0;
        private double _sensitivityIncrement = 0.1;
        private double _noisePrecision = 1.0;
        private int _numOfClasses = 2;
        private bool _labelAtStartOfLine;
        private int _totalRuns, _runsLeft;
        private bool _addBias = false;
        private bool _trainingSelected, _testingSelected, _resultsSelected;
        #endregion

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
            int[] labels = model.GetClasses();
            int correctCount = 0;
            Vector[] featureVectors = new Vector[noOfFeatures];
            BPM bpm = new BPM(numClasses, noisePrecision);
            VectorGaussian[] posteriorWeights = bpm.Train(model.GetInputs(), model.GetClasses());
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
            double logEvidence = bpm.GetLogEvidence();
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
                _trainingFilePath = openFileDialog1.FileName;
                _trainingSelected = true;
                if (_trainingSelected && _testingSelected && _resultsSelected) beginButton.Enabled = true;
            }
        }

        private void testFileSelect_Click(object sender, System.EventArgs e)
        {
            if(openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                _testFilePath = openFileDialog2.FileName;
                _testingSelected = true;
                if (_trainingSelected && _testingSelected && _resultsSelected) beginButton.Enabled = true;
            }
        }

        private void saveFileSelect_Click(object sender, System.EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _resultsFilePath = saveFileDialog1.FileName;
                _resultsSelected = true;
                if (_trainingSelected && _testingSelected && _resultsSelected) beginButton.Enabled = true;
            }
        }

        private void checkbox1_Click(object sender, System.EventArgs e)
        {
            _labelAtStartOfLine = checkBox1.Checked;
        }

        private void noOfFeatures_Changed(object sender, System.EventArgs e)
        {
            _noOfFeatures = (int)numericUpDown1.Value;
        }

        private void noOfRuns_Changed(object sender, System.EventArgs e)   
        {
            _noOfRuns = (int)numericUpDown2.Value;
        }

        private void startingSensitivity_Changed(object sender, System.EventArgs e)
        {
            _startSensitivity = Double.Parse(textBox2.Text);
        }

        private void maximumSensitivity_Changed(object sender, System.EventArgs e)
        {
            _maxSensitivity = Double.Parse(textBox3.Text);
        }

        private void sensitivityIncrement_Changed(object sender, System.EventArgs e)
        {
            _sensitivityIncrement = Double.Parse(textBox4.Text);
        }

        #endregion

        private void begin_Click(object sender, System.EventArgs e)
        {
            beginButton.Enabled = false;
            trainingFileSelect.Enabled = false;
            testFileSelect.Enabled = false;
            resultsFileSelect.Enabled = false;
            Thread calcThread = new Thread(RunTests);
            calcThread.Name = "CalcThread";
            calcThread.Priority = ThreadPriority.BelowNormal;
            _totalRuns = (int)(_noOfRuns * (1 + ((_maxSensitivity - _startSensitivity) / _sensitivityIncrement)));
            progressBar1.Maximum = _totalRuns;
            _performingCalcs = true;
            calcThread.Start();
            int prevRem = _totalRuns;
            int performedInInterval=0;
            DateTime last = DateTime.Now;
            DateTime now;
            TimeSpan diff;
            int noOfSleeps = 0;
            while (_performingCalcs)
            {
                Thread.Sleep(100);
                progressBar1.Value = (_totalRuns - _runsLeft);
                noOfSleeps++;
                if (noOfSleeps == 200)
                {
                    performedInInterval = prevRem - _runsLeft;
                    //In case of dividing by zero
                    if (performedInInterval == 0) performedInInterval = 1;
                    prevRem = _runsLeft;
                    now = DateTime.Now;
                    diff = now - last;
                    TimeSpan remainder = new TimeSpan((diff.Ticks / performedInInterval) * _runsLeft);
                    String timeEstimate = remainder.ToString();
                    textBox1.Text = (_runsLeft + " runs left of " + _totalRuns + ". Should take roughly " + timeEstimate);
                    noOfSleeps = 0;
                }
            }
            trainingFileSelect.Enabled = true;
            testFileSelect.Enabled = true;
            resultsFileSelect.Enabled = true;
        }

        private void RunTests()
        {
            StreamWriter writer = new StreamWriter(_resultsFilePath);
            //int noOfFeatures = 4;
            //int noOfTestValues = 274;
            //int noOfTestValues = 15060;
            //int noOfTrainingValues = 1098;
            //int noOfTrainingValues = 30162;
            //string trainingFilePath = @"..\..\data\banknotesdata.txt";
            //string trainingFilePath = @"..\..\data\adultTraining.txt";


            BPMDataModel trainingModel = FileUtils.ReadFile(_trainingFilePath, _labelAtStartOfLine, _noOfFeatures, _addBias);
            BPMDataModel noisyModel = FileUtils.CreateNoisyModel(trainingModel, _noisePrecision);

            //string testFile = @"..\..\data\banknotestraining.txt";
            //string testFile = @"..\..\data\adultTest.txt";s
            BPMDataModel testModel = FileUtils.ReadFile(_testFilePath, _labelAtStartOfLine, _noOfFeatures, _addBias);
            Vector[] testVectors = testModel.GetInputs();
            _runsLeft = _totalRuns;
            double accuracy = 0;
            for (double i = _startSensitivity; i <= _maxSensitivity; i = (i + _sensitivityIncrement))
            {
                //writer.WriteLine(i);
                for (int j = 0; j < _noOfRuns; j++)
                {
                    noisyModel = FileUtils.CreateNoisyModel(trainingModel, i);
                    //Set the test model data to have the same range plus max and min
                    //values as the noisy model, to normalise both data models to the same range
                    testModel.SetInputLimits(noisyModel.GetInputLimits());
                    testModel.ScaleFeatures();
                    accuracy = RunBPMGeneral(noisyModel, _numOfClasses, _noisePrecision, _addBias, testVectors, _noOfFeatures, testModel.GetClasses());
                    _runsLeft--;
                    writer.WriteLine(i + "," + accuracy);
                    //textBox1.Text = ("Done " + j + " of acc: " + i);
                }
                //progressBar1.Increment(1);
            }
            writer.Flush();
            writer.Close();
            _performingCalcs = false;
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
