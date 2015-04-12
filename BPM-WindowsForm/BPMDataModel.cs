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

        public BPMDataModel(int noOfInputs)
        {
            classes = new int[noOfInputs];
            inputsDoubles = new Vector[noOfInputs];
            ranges = new List<double>(noOfInputs);
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


    }
}