using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BayesPointMachine;
using MicrosoftResearch.Infer.Maths;

namespace BayesPointMachineForm
{
    class FileUtils
    {
        public static BPMDataModel ReadFile(string filename, bool labelAtStart, int noOfFeatures, bool addBias)
        {
            int noOfInputs = 0;
            StreamReader temp = new StreamReader(filename);
            while (!temp.EndOfStream)
            {
                temp.ReadLine();
                noOfInputs++;
            }
            temp.Dispose();
            BPMDataModel newModel = new BPMDataModel(noOfInputs);
            List<double[]> featureData = new List<double[]>();
            //Holds the upper and lower limits for each feature being analysed
            List<double[]> limits = new List<double[]>(noOfFeatures);
            for (int i = 0; i < noOfFeatures; i++)
            {
                double[] lim = new double[2];
                limits.Add(lim);
            }
            //Initialise feature data list
            for (int i = 0; i < noOfFeatures; i++)
            {
                featureData.Add(new double[noOfInputs]);
            }
            //The variables to hold the data being read in
            int[] classData = new int[noOfInputs];
            List<Vector> x = new List<Vector>();
            List<int> y = new List<int>();
            Vector[] featureVectors;
            string line;
            double[] values;
            int index;
            int labelIndex;
            int currentLocation = 0;
            using (StreamReader reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    //Allow for splitting of data in file by either tab, space or comma
                    string[] pieces = line.Split('\t', ' ', ',');
                    //This assumes class identifier AND ID no on each line
                    int n = pieces.Length - 2;
                    if (addBias)
                    {
                        values = new double[n + 1];
                        values[n] = 1;
                    }
                    else
                    {
                        values = new double[n];
                    }

                    // Read feature values and labels.
                    labelIndex = labelAtStart ? 0 : n + 1;
                    for (int i = 0; i < noOfFeatures; i++)
                    {
                        index = labelAtStart ? i + 1 : i;
                        values[i] = Double.Parse(pieces[index]);
                        featureData[i][currentLocation] = Double.Parse(pieces[index]);
                        if ((limits[i][0].Equals(null)) || (values[i] < limits[i][0]))
                        {
                            limits[i][0] = values[i];
                        }
                        if ((limits[i][1].Equals(null)) || (values[i] > limits[i][1]))
                        {
                            limits[i][1] = values[i];
                        }
                    }
                    classData[currentLocation] = Int32.Parse(pieces[labelIndex - 1]);
                    x.Add(Vector.FromArray(values));
                    y.Add(Int32.Parse(pieces[labelIndex]));
                    currentLocation++;
                }
                //Clean up resources
                reader.Dispose();
            }
            featureVectors = x.ToArray();

            newModel.SetAllVectorFeatures(featureVectors);
            newModel.SetClasses(classData);
            newModel.SetInputLimits(limits);
            return newModel;
        }

        //public List<double[]> getFeatureLimits(BPMModel model)
        //{
        //    List<double[]> limits = new List<double[]>();
        //    for (int i = 0; i < model.GetArrayFeatures().Count; i++)
        //    {
        //        double max, min;
        //        max = Math.Max(model.GetArrayFeatures()[i]);
        //        double[] lims = new double[2];
        //    }
        //}


        public static BPMDataModel CreateNoisyModel(BPMDataModel model, double epsilon)
        {
            Random generator = new Random();
            BPMDataModel noisyModel = new BPMDataModel(model.GetInputs().Length);
            Vector[] currFeatures = model.GetInputs();
            List<double> ranges = model.GetRanges();
            double[] values = new double[model.GetInputs()[0].Count];
            List<Vector> x = new List<Vector>();
            foreach (Vector vector in model.GetInputs())
            {
                for (int i = 0; i < ranges.Count; i++)
                {
                    double randomVal = (0.5 - generator.NextDouble());
                    double newVal = CreateLaplacian(epsilon, 0, vector[i], ranges[i], randomVal);
                    values[i] = newVal;
                }
                x.Add(Vector.FromArray(values));
            }
            noisyModel.SetAllVectorFeatures(x.ToArray());
            noisyModel.SetClasses(model.getClasses());
            return noisyModel;
        }

        public static double CreateLaplacian(double epsilon, double mu, double initialValue, double range, double randomValue)
        {
            double newValue;
            double b = range / epsilon;
            int sign = Math.Sign((randomValue));
            double laplNoise = (mu - b * (sign * (Math.Log(1 - 2 * (Math.Abs(randomValue))))));
            newValue = initialValue + laplNoise;
            return newValue;

        }
    }
}
