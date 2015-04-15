using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicrosoftResearch.Infer.Maths;

namespace BayesPointMachineForm
{
    class BPMDataModel
    {
        private Vector[] inputsDoubles;
        private int[] classes;
        private List<double[]> arrayInputs;
        private List<double[]> inputLimits;
        private List<double> ranges;
        private int noOfFeatures;

        public BPMDataModel(int noOfInputs, int noOfFeatures)
        {
            classes = new int[noOfInputs];
            inputsDoubles = new Vector[noOfInputs];
            ranges = new List<double>(noOfInputs);
            this.noOfFeatures = noOfFeatures;
        }
        //public void SetAFeature(double[] featureValues)
        //{
        //    inputsDoubles.Add(featureValues);
        //}

        public void SetAllVectorFeatures(Vector[] allFeatures)
        {
            this.inputsDoubles = allFeatures;
        }

        public void SetAllArrayFeatures(List<double[]> allFeatures)
        {
            this.arrayInputs = allFeatures;
        }

        public List<double[]> GetArrayFeatures()
        {
            return arrayInputs;
        }

        public Vector[] GetInputs()
        {
            return inputsDoubles;
        }

        public void SetClasses(int[] classes)
        {
            this.classes = classes;
        }

        public int[] getClasses()
        {
            return classes;
        }

        public void SetInputLimits(List<double[]> limits)
        {
            this.inputLimits = limits;
            for (int i = 0; i < limits.Count; i++)
            {
                double range = inputLimits[i][1] - inputLimits[i][0];
                ranges.Add(range);
            }
        }

        public List<double[]> GetInputLimits()
        {
            return inputLimits;
        }

        public List<double> GetRanges()
        {
            return ranges;
        }

        public void SetFeatureLimits()
        {
            for (int i = 0; i < noOfFeatures; i++)
            {
                double[] lim = new double[2];
                lim[0] = Double.NaN;
                lim[1] = Double.NaN;
                inputLimits.Add(lim);
            }
            foreach(Vector vector in inputsDoubles)
            {
                for (int j = 0; j < noOfFeatures; j++)
                {
                    if ((inputLimits[j][0].Equals(Double.NaN)) || (vector[j] < inputLimits[j][0]))
                    {
                        inputLimits[j][0] = vector[j];
                    }
                    if ((inputLimits[j][1].Equals(Double.NaN)) || (vector[j] > inputLimits[j][1]))
                    {
                        inputLimits[j][1] = vector[j];
                    }
                }
            }
            for (int i = 0; i < noOfFeatures; i++)
            {
                double range = inputLimits[i][1] - inputLimits[i][0];
                ranges.Add(range);
            }
        }


        //Scales each value in the features to between 0 and 1
        public void ScaleFeatures()
        {
            for(int j=0; j<inputsDoubles.Length; j++)
            {
                for (int i = 0; i < noOfFeatures; i++)
                {
                    inputsDoubles[j][i] = (inputsDoubles[j][i] - inputLimits[i][0])/ranges[i];
                }
            }
        }



        internal int GetNoOfFeatures()
        {
            return noOfFeatures;
        }
    }
}