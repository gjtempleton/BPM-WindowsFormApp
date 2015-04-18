﻿using BayesPointMachine;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private int _noOfFeatures = 4;
        private int _noOfRuns = 100;
        private double _startSensitivity = 0.1;
        private double _maxSensitivity = 10.0;
        private double _sensitivityIncrement = 0.1;
        private double _noisePrecision = 1.0;
        private int _numOfClasses = 2;
        private bool _labelAtStartOfLine;
        private int _totalRuns, _runsLeft;
        private bool _addBias = false;
        private bool _trainingSelected, _testingSelected, _resultsSelected;
        private static StreamWriter _writer;
        private static bool onlyWriteAggregateResults;
        private static bool writeGaussians;
        private BPMDataModel trainingModel, testModel;
        #endregion

        public Form1()
        {
            InitializeComponent();
            SetHandlers();
            progressBar1.Minimum = 0;
            Text = @"Differential Privacy Analyser";
        }

        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        private void SetHandlers()
        {
            trainingFileSelect.Click += (this.trainingFileSelect_Click);
            openFileDialog1.Filter = "Text and CSV Files (.txt, .csv)|*.txt;*.csv|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            testFileSelect.Click += (this.testFileSelect_Click);
            openFileDialog2.Filter = "Text and CSV Files (.txt, .csv)|*.txt;*.csv|All Files (*.*)|*.*";
            openFileDialog2.FilterIndex = 1;
            beginButton.Click += (this.begin_Click);
            resultsFileSelect.Click += (this.saveFileSelect_Click);
            checkBox1.Click += (this.checkbox1_Click);
            numericUpDown1.ValueChanged += (this.noOfFeatures_Changed);
            numericUpDown2.ValueChanged += (this.noOfRuns_Changed);
            numericUpDown3.ValueChanged += (noOfClasses_Changed);
            //Need at least two classes
            numericUpDown3.Minimum = 2;
            textBox2.TextChanged += (this.startingSensitivity_Changed);
            textBox3.TextChanged += (this.maximumSensitivity_Changed);
            textBox4.TextChanged += (this.sensitivityIncrement_Changed);
        }

        private static double RunBPMGeneral(BPMDataModel model, int numClasses, double noisePrecision, bool addBias, Vector[] testSet, int noOfFeatures, int[] testResults)
        {
            int correctCount = 0;
            BPM bpm = new BPM(numClasses, noisePrecision);
            VectorGaussian[] posteriorWeights = bpm.Train(model.GetInputs(), model.GetClasses());
            string actualWeights = posteriorWeights[1].ToString();
            int breakLocation = actualWeights.IndexOf("\r");
            actualWeights = actualWeights.Substring(0, breakLocation);
            if(!onlyWriteAggregateResults&&writeGaussians)_writer.WriteLine("Weights= " + actualWeights);
            Discrete[] predictions = bpm.Test(testSet);
            int i = 0;

            foreach (Discrete prediction in predictions)
            {
                if (FindMaxValPosition(prediction.GetProbs().ToArray()) == testResults[i]) correctCount++;
                i++;
            }
            double accuracy = ((double)correctCount / predictions.Length) * 100;
            double logEvidence = bpm.GetLogEvidence();
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
                saveFileDialog1.AddExtension = true;
                saveFileDialog1.DefaultExt = ".csv";
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
            try
            {
                _startSensitivity = Double.Parse(textBox2.Text);
            }
            catch (FormatException)
            {
                ShowDialog("Error in number format", "Error", false);
                textBox2.Text = _startSensitivity.ToString();
            }
        }

        private void maximumSensitivity_Changed(object sender, System.EventArgs e)
        {
            try
            {
                _maxSensitivity = Double.Parse(textBox3.Text);
            }
            catch (FormatException)
            {
                ShowDialog("Error in number format", "Error", false);
                textBox3.Text = _maxSensitivity.ToString();
            }

        }

        private void sensitivityIncrement_Changed(object sender, System.EventArgs e)
        {
            try
            {
                _sensitivityIncrement = Double.Parse(textBox4.Text);
            }
            catch (FormatException)
            {
                ShowDialog("Error in number format", "Error", false);
                textBox4.Text = _sensitivityIncrement.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void noOfClasses_Changed(object sender, System.EventArgs e)
        {
            _numOfClasses = (int) numericUpDown3.Value;
        }

        #endregion

        private void begin_Click(object sender, System.EventArgs e)
        {
            beginButton.Enabled = false;
            trainingFileSelect.Enabled = false;
            testFileSelect.Enabled = false;
            resultsFileSelect.Enabled = false;
            try
            {
                trainingModel = FileUtils.ReadFile(_trainingFilePath, _labelAtStartOfLine, _noOfFeatures, _addBias);
                testModel = FileUtils.ReadFile(_testFilePath, _labelAtStartOfLine, _noOfFeatures, _addBias);
                Thread calcThread = new Thread(RunTests);
                calcThread.Name = "CalcThread";
                calcThread.Priority = ThreadPriority.BelowNormal;
                _totalRuns = (int) (_noOfRuns*(1 + ((_maxSensitivity - _startSensitivity)/_sensitivityIncrement)));
                progressBar1.Maximum = _totalRuns;
                _performingCalcs = true;
                calcThread.Start();
                int prevRem = _totalRuns;
                int performedInInterval = 0;
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
                        TimeSpan remainder = new TimeSpan((diff.Ticks/performedInInterval)*_runsLeft);
                        String timeEstimate = remainder.ToString();
                        textBox1.Text = (_runsLeft + " runs left of " + _totalRuns + ". Should take roughly " +
                                         timeEstimate);
                        noOfSleeps = 0;
                    }
                }
                trainingFileSelect.Enabled = true;
                testFileSelect.Enabled = true;
                resultsFileSelect.Enabled = true;
            }
            catch (Exception exception)
            {
                ShowDialog("Sorry, there was an error reading the input data", "Error", true);
            }
            
        }

        private void RunTests()
        {
            _writer = new StreamWriter(_resultsFilePath);

            BPMDataModel noisyModel = FileUtils.CreateNoisyModel(trainingModel, _noisePrecision);
            Vector[] testVectors = testModel.GetInputs();
            _runsLeft = _totalRuns;
            double accuracy = 0;
            List<double> vals = new List<double>(_noOfRuns);
            double accForGroup, stdevForGroup;
            //Always write the result without any noise to begin
            //BPMDataModel temp = trainingModel;
            //temp.ScaleFeatures();
            //testModel.SetInputLimits(temp.GetInputLimits());
            //testModel.ScaleFeatures();
            accuracy = RunBPMGeneral(trainingModel, _numOfClasses, _noisePrecision, _addBias, testVectors, _noOfFeatures, testModel.GetClasses());
            _writer.WriteLine(0.0 + "," + accuracy);
            //Now loop through noisy models
            for (double i = _startSensitivity; i <= _maxSensitivity; i = (i + _sensitivityIncrement))
            {
                for (int j = 0; j < _noOfRuns; j++)
                {
                    noisyModel = FileUtils.CreateNoisyModel(trainingModel, i);
                    //Set the test model data to have the same range plus max and min
                    //values as the noisy model, to normalise both data models to the same range
                    //testModel.SetInputLimits(noisyModel.GetInputLimits());
                    //testModel.ScaleFeatures();
                    accuracy = RunBPMGeneral(noisyModel, _numOfClasses, _noisePrecision, _addBias, testVectors, _noOfFeatures, testModel.GetClasses());
                    _runsLeft--;
                    if(!onlyWriteAggregateResults) _writer.WriteLine(i + "," + accuracy);
                    else vals.Add(accuracy);
                }
                //If onlyWriteAggregateResults it calculates the mean and standard dev
                //for the results for each value of sigma
                if (onlyWriteAggregateResults)
                {
                    accForGroup = FileUtils.Mean(vals);
                    stdevForGroup = FileUtils.StandardDeviation(vals);
                    vals.Clear();
                    _writer.WriteLine(i + "," + accForGroup + "," + stdevForGroup);
                }
            }
            _writer.Flush();
            _writer.Close();
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

        public void ShowDialog(string text, string title, bool fileError)
        {
            Form messageForm = new Form();
            messageForm.Width = 300;
            messageForm.Height = 100;
            messageForm.Text = title;
            //Create a text label for it to pass the user the message
            Label textLabel = new Label() { Left = 50, Top = 20, Text = text };
            Button dismissButton = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70 };
            dismissButton.Click += (sender, e) => { messageForm.Close(); };
            //If caused by an error force the user to reselect the two files
            if (fileError)
            {
                _trainingSelected = false;
                _testingSelected = false;
            }
            messageForm.Controls.Add(textLabel);
            messageForm.ShowDialog();
        }


    }
}
