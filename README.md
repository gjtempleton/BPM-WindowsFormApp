# BPM-WindowsFormApp
Windows Form App for automated testing of Laplacian noise addition to datasets. Automates user defined epsilon values for the noise to allow users to evaluate theeffect of differing values. THIS SOFTWARE IS GOVERNED BY THE TERMS OF THE LICENSE IN 'InferNetLicense.rtf'
A number of issues exist with the software currently: little to no error checking of user input or input data; no calculation of evidence values for models.

Assumptions are made on the input data formats, including that data is augmented by a id number on each row. Examples of data from the banknote authentication dataset at: http://archive.ics.uci.edu/ml/datasets/banknote+authentication

Note that due to the licensing terms the required Infer Compiler DLL will need to be downloaded from: http://research.microsoft.com/en-us/um/cambridge/projects/infernet/ before building.
