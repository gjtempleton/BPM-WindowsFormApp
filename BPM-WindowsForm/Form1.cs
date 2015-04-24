﻿using BayesPointMachine;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private bool _performingCalcs, _memoryException;
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
        private static bool _onlyWriteAggregateResults, _writeGaussians;
        private BPMDataModel _trainingModel, _testModel;
        private bool appendToFile;
        private static BPM bpm;
        private double epsilon;
        private BackgroundWorker bw = new BackgroundWorker();
        int prevRem;
        int performedInInterval;
        DateTime last = DateTime.Now;
        DateTime now;
        TimeSpan diff;
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
            trainingFileSelect.Click += (trainingFileSelect_Click);
            openFileDialog1.Filter = @"Text and CSV Files (.txt, .csv)|*.txt;*.csv|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            testFileSelect.Click += (testFileSelect_Click);
            openFileDialog2.Filter = @"Text and CSV Files (.txt, .csv)|*.txt;*.csv|All Files (*.*)|*.*";
            openFileDialog2.FilterIndex = 1;
            beginButton.Click += (begin_Click);
            resultsFileSelect.Click += (saveFileSelect_Click);
            checkBox1.Click += (checkbox1_Click);
            numericUpDown1.ValueChanged += (noOfFeatures_Changed);
            numericUpDown2.ValueChanged += (noOfRuns_Changed);
            numericUpDown3.ValueChanged += (noOfClasses_Changed);
            //Need at least two classes
            numericUpDown3.Minimum = 2;
            textBox2.TextChanged += (startingSensitivity_Changed);
            textBox3.TextChanged += (maximumSensitivity_Changed);
            textBox4.TextChanged += (sensitivityIncrement_Changed);
            checkBox2.Click += (aggregateResults_Changed);
            checkBox3.Click += (writeGaussians_Changed);
        }

        private static double RunBPMGeneral(BPMDataModel model, int numClasses, double noisePrecision, bool addBias, Vector[] testSet, int[] testResults)
        {
            int correctCount = 0;
            VectorGaussian[] posteriorWeights = bpm.Train(model.GetInputs(), model.GetClasses());
            string actualWeights = posteriorWeights[1].ToString();
            int breakLocation = actualWeights.IndexOf("\r", StringComparison.Ordinal);
            actualWeights = actualWeights.Substring(0, breakLocation);
            if (!_onlyWriteAggregateResults && _writeGaussians) _writer.WriteLine("Weights= " + actualWeights);
            Discrete[] predictions = bpm.Test(testSet);
            int i = 0;

            foreach (Discrete prediction in predictions)
            {
                if (FindMaxValPosition(prediction.GetProbs().ToArray()) == testResults[i]) correctCount++;
                i++;
            }
            double accuracy = ((double)correctCount / predictions.Length) * 100;
            //double logEvidence = bpm.GetLogEvidence();
            return accuracy;
        }

        #region handlers

        private void trainingFileSelect_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _trainingFilePath = openFileDialog1.FileName;
                _trainingSelected = true;
                if (_trainingSelected && _testingSelected && _resultsSelected) beginButton.Enabled = true;
            }
        }

        private void testFileSelect_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                _testFilePath = openFileDialog2.FileName;
                _testingSelected = true;
                if (_trainingSelected && _testingSelected && _resultsSelected) beginButton.Enabled = true;
            }
        }

        private void saveFileSelect_Click(object sender, EventArgs e)
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

        private void checkbox1_Click(object sender, EventArgs e)
        {
            _labelAtStartOfLine = checkBox1.Checked;
        }

        private void noOfFeatures_Changed(object sender, EventArgs e)
        {
            _noOfFeatures = (int)numericUpDown1.Value;
        }

        private void noOfRuns_Changed(object sender, EventArgs e)
        {
            _noOfRuns = (int)numericUpDown2.Value;
        }

        private void startingSensitivity_Changed(object sender, EventArgs e)
        {
            try
            {
                _startSensitivity = Double.Parse(textBox2.Text);
            }
            catch (FormatException)
            {
                ShowDialog("Error in number format", "Error", false);
                textBox2.Text = _startSensitivity.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void maximumSensitivity_Changed(object sender, EventArgs e)
        {
            try
            {
                _maxSensitivity = Double.Parse(textBox3.Text);
            }
            catch (FormatException)
            {
                ShowDialog("Error in number format", "Error", false);
                textBox3.Text = _maxSensitivity.ToString(CultureInfo.InvariantCulture);
            }

        }

        private void sensitivityIncrement_Changed(object sender, EventArgs e)
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

        private void noOfClasses_Changed(object sender, EventArgs e)
        {
            _numOfClasses = (int)numericUpDown3.Value;
        }

        private void aggregateResults_Changed(object sender, EventArgs e)
        {
            _onlyWriteAggregateResults = checkBox2.Checked;
        }

        private void writeGaussians_Changed(object sender, EventArgs e)
        {
            _writeGaussians = checkBox3.Checked;
        }

        #endregion

        private void begin_Click(object sender, EventArgs e)
        {
            if (_performingCalcs)
            {
                //Disable input changes
                ChangeStatusOfInputs(false);
                beginButton.Text = @"Cancel";
                bpm = new BPM(_numOfClasses, _noisePrecision);
                try
                {
                    _trainingModel = FileUtils.ReadFile(_trainingFilePath, _labelAtStartOfLine, _noOfFeatures, _addBias);
                    _testModel = FileUtils.ReadFile(_testFilePath, _labelAtStartOfLine, _noOfFeatures, _addBias);
                    _writer = new StreamWriter(_resultsFilePath, appendToFile);
                    _totalRuns = (int) (_noOfRuns*(1 + ((_maxSensitivity - _startSensitivity)/_sensitivityIncrement)));
                    bw.WorkerReportsProgress = true;
                    bw.WorkerSupportsCancellation = true;
                    bw.DoWork += new DoWorkEventHandler(bw_DoWork);
                    bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
                    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
                    bw.RunWorkerAsync();
                    //Thread calcThread = new Thread(RunTests);
                    //calcThread.Name = "CalcThread";
                    //calcThread.Priority = ThreadPriority.Lowest;

                    progressBar1.Maximum = _totalRuns;
                    _performingCalcs = true;
                    //calcThread.Start();
                    prevRem = _totalRuns;

                }
                catch (Exception exception)
                {
                    ShowDialog("Sorry, there was an error reading the input data" + exception.GetType(), "Error", true);
                }
            }
            else
            {
                bw.CancelAsync();
                beginButton.Text = @"Begin processing";
                //Tidy up
                statusLabel.Text = @"";
            }

        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            BPMDataModel noisyModel; // = FileUtils.CreateNoisyModel(_trainingModel, _noisePrecision);
            Vector[] testVectors = _testModel.GetInputs();
            _runsLeft = _totalRuns;
            double accuracy;
            List<double> vals = new List<double>(_noOfRuns);
            double accForGroup, stdevForGroup;
            //Always write the result without any noise to begin
            //BPMDataModel temp = trainingModel;
            //temp.ScaleFeatures();
            //testModel.SetInputLimits(temp.GetInputLimits());
            //testModel.ScaleFeatures();
            accuracy = RunBPMGeneral(_trainingModel, _numOfClasses, _noisePrecision, _addBias, testVectors,
                _testModel.GetClasses());
            _writer.WriteLine(0.0 + "," + accuracy);
            //Now loop through noisy models
            for (double i = _startSensitivity; i <= _maxSensitivity; i = (i + _sensitivityIncrement))
            {
                //Round i to nearest (_sensitivityIncrement) to allow for floating point error
                //double roundingVal = 1/_sensitivityIncrement;
                i = Math.Round(i, 2, MidpointRounding.AwayFromZero);
                epsilon = i;
                //i = (Math.Floor((i * roundingVal) + (roundingVal / 2)) * _sensitivityIncrement);
                for (int j = 0; j < _noOfRuns; j++)
                {
                    if ((worker.CancellationPending))
                    {
                        e.Cancel = true;
                        //Break out of both loops
                        i = _maxSensitivity + 1;
                        break;
                    }
                    System.Threading.Thread.CurrentThread.Join(10);
                    noisyModel = FileUtils.CreateNoisyModel(_trainingModel, i);
                    //Set the test model data to have the same range plus max and min
                    //values as the noisy model, to normalise both data models to the same range
                    //testModel.SetInputLimits(noisyModel.GetInputLimits());
                    //testModel.ScaleFeatures();
                    accuracy = RunBPMGeneral(noisyModel, _numOfClasses, _noisePrecision, _addBias, testVectors,
                        _testModel.GetClasses());
                    _runsLeft--;
                    worker.ReportProgress(_runsLeft);
                    //if (_runsLeft == 9970) ShowDialog("Sorry, there was an error reading the input data", "Error", true);
                    if (!_onlyWriteAggregateResults) _writer.WriteLine(i + "," + accuracy);
                    else vals.Add(accuracy);
                }
                //If onlyWriteAggregateResults it calculates the mean and standard dev
                //for the results for each value of sigma
                if (_onlyWriteAggregateResults)
                {
                    accForGroup = FileUtils.Mean(vals);
                    stdevForGroup = FileUtils.StandardDeviation(vals);
                    vals.Clear();
                    _writer.WriteLine(i + "," + accForGroup + "," + stdevForGroup);
                }
                _writer.Flush();
            }
            _writer.Flush();
            _writer.Close();
            _performingCalcs = false;
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = (_totalRuns - e.ProgressPercentage);
            statusLabel.Text = string.Format("PErforming inference for epsilon = {0}...", epsilon);
            if ((progressBar1.Value) % 10 == 0)
            {
                performedInInterval = prevRem - _runsLeft;
                //In case of dividing by zero
                if (performedInInterval == 0) performedInInterval = 1;
                prevRem = _runsLeft;
                now = DateTime.Now;
                diff = now - last;
                last = now;
                TimeSpan remainder = new TimeSpan((diff.Ticks / performedInInterval) * _runsLeft);
                String timeEstimate = remainder.ToString();
                textBox1.Text = (_runsLeft + @" runs left of " + _totalRuns + @". Should take roughly " +
                                 timeEstimate);
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                textBox1.Text = "Canceled!";
            }
            else
            {
                textBox1.Text = "Done!";
            }
            ChangeStatusOfInputs(true);
            
        }

        private void RunTests()
        {
            try
            {
                BPMDataModel noisyModel; // = FileUtils.CreateNoisyModel(_trainingModel, _noisePrecision);
                Vector[] testVectors = _testModel.GetInputs();
                _runsLeft = _totalRuns;
                double accuracy;
                List<double> vals = new List<double>(_noOfRuns);
                double accForGroup, stdevForGroup;
                //Always write the result without any noise to begin
                //BPMDataModel temp = trainingModel;
                //temp.ScaleFeatures();
                //testModel.SetInputLimits(temp.GetInputLimits());
                //testModel.ScaleFeatures();
                accuracy = RunBPMGeneral(_trainingModel, _numOfClasses, _noisePrecision, _addBias, testVectors,
                    _testModel.GetClasses());
                _writer.WriteLine(0.0 + "," + accuracy);
                //Now loop through noisy models
                for (double i = _startSensitivity; i <= _maxSensitivity; i = (i + _sensitivityIncrement))
                {
                    //Round i to nearest (_sensitivityIncrement) to allow for floating point error
                    //double roundingVal = 1/_sensitivityIncrement;
                    i = Math.Round(i, 2, MidpointRounding.AwayFromZero);
                    //i = (Math.Floor((i * roundingVal) + (roundingVal / 2)) * _sensitivityIncrement);
                    for (int j = 0; j < _noOfRuns; j++)
                    {
                        System.Threading.Thread.CurrentThread.Join(10);
                        noisyModel = FileUtils.CreateNoisyModel(_trainingModel, i);
                        //Set the test model data to have the same range plus max and min
                        //values as the noisy model, to normalise both data models to the same range
                        //testModel.SetInputLimits(noisyModel.GetInputLimits());
                        //testModel.ScaleFeatures();
                        accuracy = RunBPMGeneral(noisyModel, _numOfClasses, _noisePrecision, _addBias, testVectors,
                            _testModel.GetClasses());
                        _runsLeft--;
                        //if (_runsLeft == 9970) ShowDialog("Sorry, there was an error reading the input data", "Error", true);
                        if (!_onlyWriteAggregateResults) _writer.WriteLine(i + "," + accuracy);
                        else vals.Add(accuracy);
                    }
                    //If onlyWriteAggregateResults it calculates the mean and standard dev
                    //for the results for each value of sigma
                    if (_onlyWriteAggregateResults)
                    {
                        accForGroup = FileUtils.Mean(vals);
                        stdevForGroup = FileUtils.StandardDeviation(vals);
                        vals.Clear();
                        _writer.WriteLine(i + "," + accForGroup + "," + stdevForGroup);
                    }
                    _writer.Flush();
                }
                _writer.Flush();
                _writer.Close();
                _performingCalcs = false;
            }
            catch (OutOfMemoryException excep)
            {
                _writer.Flush();
                _writer.Close();
                _performingCalcs = false;
                _memoryException = true;
            }
        }

        private static int FindMaxValPosition(double[] values)
        {
            double maxValue = values.Max();
            int i = 0;
            foreach (double val in values)
            {
                if (Math.Abs(val - maxValue) < 0.1)
                    return i;
                i++;
            }
            return -1;
        }

        public void ShowDialog(string text, string title, bool fileError)
        {
            Form messageForm = new Form();
            messageForm.Width = 1300;
            messageForm.Height = 500;
            messageForm.Text = title;
            //Create a text label for it to pass the user the message
            Label textLabel = new Label() { Left = 50, Top = 20, Text = text, Width = 1200, Height = 460 };
            Button dismissButton = new Button() { Text = @"Ok", Left = 350, Width = 100, Top = 70 };
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

        private void ChangeStatusOfInputs(bool reEnable)
        {
            trainingFileSelect.Enabled = reEnable;
            testFileSelect.Enabled = reEnable;
            resultsFileSelect.Enabled = reEnable;
            numericUpDown1.Enabled = reEnable;
            numericUpDown2.Enabled = reEnable;
            numericUpDown3.Enabled = reEnable;
            textBox2.Enabled = reEnable;
            textBox3.Enabled = reEnable;
            textBox4.Enabled = reEnable;
            checkBox1.Enabled = reEnable;
            checkBox2.Enabled = reEnable;
            checkBox3.Enabled = reEnable;
        }


    }
}