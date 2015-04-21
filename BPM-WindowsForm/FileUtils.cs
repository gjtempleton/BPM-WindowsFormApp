using System;
using System.Collections.Generic;
using System.IO;
using MicrosoftResearch.Infer.Maths;

namespace BayesPointMachineForm
{
    internal class FileUtils
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
            BPMDataModel newModel = new BPMDataModel(noOfInputs, noOfFeatures);
            List<double[]> featureData = new List<double[]>();
            //Holds the upper and lower limits for each feature being analysed
            List<double[]> limits = new List<double[]>(noOfFeatures);
            for (int i = 0; i < noOfFeatures; i++)
            {
                double[] lim = new double[2];
                lim[0] = Double.NaN;
                lim[1] = Double.NaN;
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
                    if (line != null)
                    {
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
                            if ((limits[i][0].Equals(Double.NaN)) || (values[i] < limits[i][0]))
                            {
                                limits[i][0] = values[i];
                            }
                            if ((limits[i][1].Equals(Double.NaN)) || (values[i] > limits[i][1]))
                            {
                                limits[i][1] = values[i];
                            }
                        }
                        classData[currentLocation] = Int32.Parse(pieces[labelIndex - 1]);
                        x.Add(Vector.FromArray(values));
                    }
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


        public static BPMDataModel CreateNoisyModel(BPMDataModel model, double epsilon)
        {
            Random generator = new Random();
            BPMDataModel noisyModel = new BPMDataModel(model.GetInputs().Length, model.GetNoOfFeatures());
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
            noisyModel.SetClasses(model.GetClasses());
            //noisyModel.CalculateFeatureLimits();
            //noisyModel.ScaleFeatures();
            return noisyModel;
        }

        public static double CreateLaplacian(double epsilon, double mu, double initialValue, double range,
            double randomValue)
        {
            double newValue;
            double b = range/epsilon;
            int sign = Math.Sign((randomValue));
            double laplNoise = (mu - b*(sign*(Math.Log(1 - 2*(Math.Abs(randomValue))))));
            newValue = initialValue + laplNoise;
            return newValue;
        }

        //Use Welford's method
        public static double StandardDeviation(List<double> valueList)
        {
            double m = 0.0;
            double s = 0.0;
            int k = 1;
            foreach (double value in valueList)
            {
                double tmpM = m;
                m += (value - tmpM)/k;
                s += (value - tmpM)*(value - m);
                k++;
            }
            //Use k-1 as have whole population, not just sample
            return Math.Sqrt(s/(k - 1));
        }

        public static double Mean(List<double> valueList)
        {
            double sum = 0.0;
            foreach (double value in valueList)
            {
                sum += value;
            }
            return sum/valueList.Count;
        }

        internal static double[] CleanupFile(string filePath, int noOfRuns)
        {
            double[] results;
            //StringReader reader = new StringReader(filePath);
            string tempFilePath = filePath + ".tmp";
            StreamWriter writeTemp = new StreamWriter(tempFilePath);
            string line;
            double final = Double.NaN;
            int count = 1;
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    //Allow for splitting of data in file by either tab, space or comma
                    if (line != null)
                    {
                        writeTemp.WriteLine(line);
                        string[] pieces = line.Split(',');
                        if (final == Double.Parse(pieces[0]))
                        {
                            count++;
                        }
                        else
                        {
                            final = Double.Parse(pieces[0]);
                            count = 1;
                        }
                        
                    }
                }
            }
            StreamWriter writer = new StreamWriter(filePath);
            using (StreamReader reader = new StreamReader(tempFilePath))
            {
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        string[] pieces = line.Split(',');
                        //If the number of results for the last epsilon value
                        //Is < the noOfRuns then discard them all, otherwise write to the file
                        if (!(count != noOfRuns && final==Double.Parse(pieces[0])))
                        {
                            writer.WriteLine(line);
                        }
                    }
                }
            }
            results = new double[2] {final, count};
            return results;
        }
    }
}
