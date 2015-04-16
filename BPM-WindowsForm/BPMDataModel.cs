using System;
using System.Collections.Generic;
using MicrosoftResearch.Infer.Maths;

namespace BayesPointMachineForm
{
    class BPMDataModel
    {
        private Vector[] _inputsDoubles;
        private int[] _classes;
        private List<double[]> _arrayInputs;
        private List<double[]> _inputLimits;
        private List<double> _ranges;
        private int _noOfFeatures;

        public BPMDataModel(int noOfInputs, int noOfFeatures)
        {
            _classes = new int[noOfInputs];
            _inputsDoubles = new Vector[noOfInputs];
            _ranges = new List<double>(noOfInputs);
            _noOfFeatures = noOfFeatures;
        }
        //public void SetAFeature(double[] featureValues)
        //{
        //    inputsDoubles.Add(featureValues);
        //}

        public void SetAllVectorFeatures(Vector[] allFeatures)
        {
            _inputsDoubles = allFeatures;
        }

        public void SetAllArrayFeatures(List<double[]> allFeatures)
        {
            _arrayInputs = allFeatures;
        }

        public List<double[]> GetArrayFeatures()
        {
            return _arrayInputs;
        }

        public Vector[] GetInputs()
        {
            return _inputsDoubles;
        }

        public void SetClasses(int[] classes)
        {
            _classes = classes;
        }

        public int[] GetClasses()
        {
            return _classes;
        }

        public void SetInputLimits(List<double[]> limits)
        {
            _inputLimits = limits;
            for (int i = 0; i < limits.Count; i++)
            {
                double range = _inputLimits[i][1] - _inputLimits[i][0];
                _ranges.Add(range);
            }
        }

        public List<double[]> GetInputLimits()
        {
            return _inputLimits;
        }

        public List<double> GetRanges()
        {
            return _ranges;
        }

        public void CalculateFeatureLimits()
        {
            _inputLimits = new List<double[]>();
            for (int i = 0; i < _noOfFeatures; i++)
            {
                double[] lim = new double[2];
                lim[0] = Double.NaN;
                lim[1] = Double.NaN;
                _inputLimits.Add(lim);
            }
            foreach(Vector vector in _inputsDoubles)
            {
                for (int j = 0; j < _noOfFeatures; j++)
                {
                    if ((_inputLimits[j][0].Equals(Double.NaN)) || (vector[j] < _inputLimits[j][0]))
                    {
                        _inputLimits[j][0] = vector[j];
                    }
                    if ((_inputLimits[j][1].Equals(Double.NaN)) || (vector[j] > _inputLimits[j][1]))
                    {
                        _inputLimits[j][1] = vector[j];
                    }
                }
            }
            for (int i = 0; i < _noOfFeatures; i++)
            {
                double range = _inputLimits[i][1] - _inputLimits[i][0];
                _ranges.Add(range);
            }
        }


        //Scales each value in the features to between 0 and 1
        public void ScaleFeatures()
        {
            for(int j=0; j<_inputsDoubles.Length; j++)
            {
                for (int i = 0; i < _noOfFeatures; i++)
                {
                    _inputsDoubles[j][i] = (_inputsDoubles[j][i] - _inputLimits[i][0])/_ranges[i];
                }
            }
        }



        internal int GetNoOfFeatures()
        {
            return _noOfFeatures;
        }
    }
}