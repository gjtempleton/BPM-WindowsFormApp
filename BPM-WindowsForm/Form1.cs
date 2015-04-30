using BayesPointMachine;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BayesPointMachineForm
{
    public partial class Form1 : Form
    {
        #region variables
        private bool _performingCalcs;
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
        private bool _appendToFile;
        private static BPM _bpm;
        private double _epsilon;
        private BackgroundWorker bw = new BackgroundWorker();
        private int _prevRem;
        private int _performedInInterval;
        private DateTime _last;
        private DateTime _now;
        private TimeSpan _diff;
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
            textBox2.KeyPress += numericTextBox_KeyPress;
            textBox2.Leave += (startingSensitivity_Changed);
            textBox3.KeyPress += numericTextBox_KeyPress;
            textBox3.Leave += (maximumSensitivity_Changed);
            textBox4.KeyPress += numericTextBox_KeyPress;
            textBox4.Leave += (sensitivityIncrement_Changed);
            checkBox2.Click += (aggregateResults_Changed);
            checkBox3.Click += (writeGaussians_Changed);
        }


        private static double RunBPMGeneral(BPMDataModel model, bool addBias, Vector[] testSet, int[] testResults)
        {
            int correctCount = 0;
            VectorGaussian[] posteriorWeights = _bpm.Train(model.GetInputs(), model.GetClasses());
            string actualWeights = posteriorWeights[1].ToString();
            int breakLocation = actualWeights.IndexOf("\r", StringComparison.Ordinal);
            actualWeights = actualWeights.Substring(0, breakLocation);
            if (!_onlyWriteAggregateResults && _writeGaussians) _writer.WriteLine("Weights= " + actualWeights);
            Discrete[] predictions = _bpm.Test(testSet);
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

        void numericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (Char)Keys.Back &&  e.KeyChar != (Char)Keys.Delete) 
            {
                e.Handled = (!char.IsNumber(e.KeyChar) && !e.KeyChar.ToString().Equals("."));
            }
        }

        private void startingSensitivity_Changed(object sender, EventArgs e)
        {
            if (textBox2.TextLength == 0)
            {
                textBox2.Text = _startSensitivity.ToString(CultureInfo.InvariantCulture);
            }
            else _startSensitivity = Double.Parse(textBox2.Text);
        }

        private void maximumSensitivity_Changed(object sender, EventArgs e)
        {
            if (textBox3.TextLength == 0)
            {
                textBox3.Text = _maxSensitivity.ToString(CultureInfo.InvariantCulture);
            }
            else _maxSensitivity = Double.Parse(textBox3.Text);
        }

        private void sensitivityIncrement_Changed(object sender, EventArgs e)
        {
            if (textBox4.TextLength == 0)
            {
                textBox4.Text = _sensitivityIncrement.ToString(CultureInfo.InvariantCulture);
            }
            else _sensitivityIncrement = Double.Parse(textBox2.Text);
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

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            _appendToFile = checkBox4.Checked;
        }

        #endregion

        private void begin_Click(object sender, EventArgs e)
        {
            if (_maxSensitivity < _startSensitivity)
            {
                ShowDialog(@"The maximum sensitivity must be greater than or equal to the minimum sensitivity.",
                    "Error", false);
                return;
            }
            if (!_performingCalcs)
            {
                //Disable input changes
                ChangeStatusOfInputs(false);
                beginButton.Text = @"Cancel";
                _bpm = new BPM(_numOfClasses, _noisePrecision);
                try
                {
                    _trainingModel = FileUtils.ReadFile(_trainingFilePath, _labelAtStartOfLine, _noOfFeatures, _addBias);
                    _testModel = FileUtils.ReadFile(_testFilePath, _labelAtStartOfLine, _noOfFeatures, _addBias);
                    _writer = new StreamWriter(_resultsFilePath, _appendToFile);
                    _totalRuns = (int) Math.Ceiling(_noOfRuns*(1 + ((_maxSensitivity - _startSensitivity)/_sensitivityIncrement)));
                    _last = DateTime.Now;
                    bw.WorkerReportsProgress = true;
                    bw.WorkerSupportsCancellation = true;
                    bw.DoWork += bw_DoWork;
                    bw.ProgressChanged += bw_ProgressChanged;
                    bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                    bw.RunWorkerAsync();
                    progressBar1.Maximum = _totalRuns;
                    _performingCalcs = true;
                    _prevRem = _totalRuns;

                }
                catch (Exception exception)
                {
                    ShowDialog("Sorry, there was an error reading the input data" + exception.GetType(), "Error", true);
                    beginButton.Text = @"Begin processing";
                    ChangeStatusOfInputs(true);
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
            BPMDataModel noisyModel;
            Vector[] testVectors = _testModel.GetInputs();
            _runsLeft = _totalRuns;
            double accuracy;
            List<double> vals = new List<double>(_noOfRuns);
            double accForGroup, stdevForGroup;
            #region Scaling currently doesn't work
            //BPMDataModel temp = trainingModel;
            //temp.ScaleFeatures();
            //testModel.SetInputLimits(temp.GetInputLimits());
            //testModel.ScaleFeatures();
            #endregion
            //Assume that if appending to file accuracy for epsilon = 0 is already calculated
            if (!_appendToFile)
            {
                accuracy = RunBPMGeneral(_trainingModel, _addBias, testVectors,
                    _testModel.GetClasses());

                _writer.WriteLine(0.0 + "," + accuracy);
            }
            //Now loop through noisy models
            for (double i = _startSensitivity; i <= _maxSensitivity; i = (i + _sensitivityIncrement))
            {
                //Round i to nearest (_sensitivityIncrement) to allow for floating point error
                //Assumes user will not want to increment in steps smaller than 0.001
                i = Math.Round(i, 3, MidpointRounding.AwayFromZero);
                _epsilon = i;
                for (int j = 0; j < _noOfRuns; j++)
                {
                    if (worker != null && (worker.CancellationPending))
                    {
                        e.Cancel = true;
                        //Break out of both loops
                        i = _maxSensitivity + 1;
                        break;
                    }
                    noisyModel = FileUtils.CreateNoisyModel(_trainingModel, i);
                    #region Scaling features again
                    //Set the test model data to have the same range plus max and min
                    //values as the noisy model, to normalise both data models to the same range
                    //testModel.SetInputLimits(noisyModel.GetInputLimits());
                    //testModel.ScaleFeatures();
                    #endregion
                    accuracy = RunBPMGeneral(noisyModel, _addBias, testVectors,
                        _testModel.GetClasses());
                    _runsLeft--;
                    if (worker != null) worker.ReportProgress(_runsLeft);
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
            statusLabel.Text = string.Format("Performing inference for epsilon = {0}...", _epsilon);
            if ((progressBar1.Value) % 10 == 0)
            {
                _performedInInterval = _prevRem - _runsLeft;
                //In case of dividing by zero
                if (_performedInInterval == 0) _performedInInterval = 1;
                _prevRem = _runsLeft;
                _now = DateTime.Now;
                _diff = _now - _last;
                _last = _now;
                TimeSpan remainder = new TimeSpan((_diff.Ticks / _performedInInterval) * _runsLeft);
                String timeEstimate = String.Format("{0} day{1} {2} hour{3} {4} minute{5} {6} second{7}",
                    remainder.Days,
                    remainder.Days == 1 ? "" : "s",
                    remainder.Hours,
                    remainder.Hours == 1 ? "" : "s",
                    remainder.Minutes,
                    remainder.Minutes == 1 ? "" : "s",
                    remainder.Seconds,
                    remainder.Seconds == 1 ? "" : "s");
                textBox1.Text = (_runsLeft + @" runs left of " + _totalRuns + @". Should take roughly " +
                                 timeEstimate);
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                textBox1.Text = @"Canceled!";
            }
            else if (!(e.Error == null))
            {
                textBox1.Text = (@"Error: " + e.Error.Message + @"
Performing cleanup of results file now.");
                int noOfLines;
                if (!_onlyWriteAggregateResults) noOfLines = _writeGaussians ? _noOfRuns : _noOfRuns*2;
                else noOfLines = _writeGaussians ? 2 : _noOfRuns + 2;
                double[] cleanupResults = FileUtils.CleanupFile(_resultsFilePath, noOfLines);
                if (cleanupResults[1] == _noOfRuns) _startSensitivity = (cleanupResults[0] + _sensitivityIncrement);
                else _startSensitivity = cleanupResults[0];
                checkBox4.Checked = true;
                checkBox4.Update();
                textBox4.Text = _startSensitivity.ToString();
                textBox4.Update();
                textBox1.Text = (string.Format("Cleanup done, can try running again now and will append to end of file."));
            }
            else
            {
                textBox1.Text = @"Done!";
                beginButton.Text = @"Begin processing";
                statusLabel.Text = @"";
                ShowDialog(string.Format("Inference finished, results saved to {0}", _resultsFilePath), "Done!", false);
            }
            ChangeStatusOfInputs(true);
            
        }

        private static int FindMaxValPosition(double[] values)
        {
            double maxValue = values.Max();
            int i = 0;
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
            messageForm.Height = 200;
            messageForm.Text = title;
            //Create a text label for it to pass the user the message
            Label textLabel = new Label() { Left = 50, Top = 20, Text = text, Width = 200, Height = 60 };
            Button dismissButton = new Button() { Text = @"Ok", Left = 100, Width = 100, Top = 80 };
            dismissButton.Click += (sender, e) => { messageForm.Close(); };
            //If caused by an error force the user to reselect the two files
            if (fileError)
            {
                _trainingSelected = false;
                _testingSelected = false;
            }
            messageForm.Controls.Add(textLabel);
            messageForm.Controls.Add(dismissButton);
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
            checkBox4.Enabled = reEnable;
        }

    }
}