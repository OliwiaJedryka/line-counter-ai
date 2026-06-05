using System;
using System.Collections.Generic;
using System.IO;

namespace LineCounterApp
{
    public class NeuralNetwork
    {
        private int inputSize, hiddenSize;
        private double[,] weightsInputHidden;
        private double[] weightsHiddenOutput;
        private Random rnd = new Random();

        public NeuralNetwork(int inputSize, int hiddenSize)
        {
            this.inputSize = inputSize; this.hiddenSize = hiddenSize;
            weightsInputHidden = new double[inputSize, hiddenSize];
            weightsHiddenOutput = new double[hiddenSize];
            InitializeWeights();
        }

        private void InitializeWeights()
        {
            for (int i = 0; i < inputSize; i++)
                for (int j = 0; j < hiddenSize; j++) weightsInputHidden[i, j] = rnd.NextDouble() * 2 - 1;
            for (int i = 0; i < hiddenSize; i++) weightsHiddenOutput[i] = rnd.NextDouble() * 2 - 1;
        }

public void Train(List<double[]> inputs, List<double> targets, int epochs, double lr, Action<string> logger)
{
    for (int e = 0; e < epochs; e++)
    {
        double totalError = 0;
        for (int i = 0; i < inputs.Count; i++)
        {
            // 1. FORWARD PASS
            double[] hidden = GetHiddenLayer(inputs[i]);
            double output = GetOutput(hidden);
            
            // Usunąłem z kodu Dropout, ponieważ w najprostszych sieciach implementowanych od zera
            // często bardziej szkodzi niż pomaga, jeśli nie przeskalujesz wag po procesie uczenia.

            double error = targets[i] - output;
            totalError += Math.Abs(error);

            // 2. BACKWARD PASS
            // Pochodna funkcji Sigmoid dla wyjścia: output * (1 - output)
            double dOutput = error * (output * (1.0 - output));

            // Obliczamy błędy dla warstwy ukrytej PRZED aktualizacją wag wyjściowych!
            double[] hiddenErrors = new double[hiddenSize];
            for (int h = 0; h < hiddenSize; h++)
            {
                // Pochodna funkcji Tanh: (1 - tanh^2)
                hiddenErrors[h] = dOutput * weightsHiddenOutput[h] * (1.0 - hidden[h] * hidden[h]);
            }

            // 3. AKTUALIZACJA WAG
            // Aktualizacja wag wyjścia
            for (int h = 0; h < hiddenSize; h++)
            {
                weightsHiddenOutput[h] += lr * dOutput * hidden[h];
            }

            // Aktualizacja wag wejścia
            for (int h = 0; h < hiddenSize; h++)
            {
                for (int inp = 0; inp < inputSize; inp++)
                {
                    weightsInputHidden[inp, h] += lr * hiddenErrors[h] * inputs[i][inp];
                }
            }
        }
        
        if (e % 20 == 0 || e == epochs - 1) 
            logger($"Trenowanie: Epoka {e}/{epochs}, Średni błąd: {totalError / inputs.Count:F4}");
    }
}

        public void SaveWeights(string path)
{
    using var writer = new StreamWriter(path);
    // Zapis w ustandaryzowanym formacie (z kropką)
    for (int i = 0; i < inputSize; i++)
        for (int j = 0; j < hiddenSize; j++) 
            writer.WriteLine(weightsInputHidden[i, j].ToString(System.Globalization.CultureInfo.InvariantCulture));
            
    for (int i = 0; i < hiddenSize; i++) 
        writer.WriteLine(weightsHiddenOutput[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
}

public void LoadWeights(string path)
{
    if (!File.Exists(path)) return;
    var lines = File.ReadAllLines(path);
    
    // Zabezpieczenie przed błędem po zmianie rozmiaru sieci!
    if (lines.Length != (inputSize * hiddenSize) + hiddenSize)
    {
        throw new Exception("Plik z wagami nie pasuje do aktualnej struktury sieci (inna liczba neuronów). Należy wykasować stary plik i wytrenować od nowa!");
    }

    int idx = 0;
    for (int i = 0; i < inputSize; i++)
        for (int j = 0; j < hiddenSize; j++) 
            weightsInputHidden[i, j] = double.Parse(lines[idx++], System.Globalization.CultureInfo.InvariantCulture);
            
    for (int i = 0; i < hiddenSize; i++) 
        weightsHiddenOutput[i] = double.Parse(lines[idx++], System.Globalization.CultureInfo.InvariantCulture);
}

        public double Forward(double[] input) => GetOutput(GetHiddenLayer(input));
        private double[] GetHiddenLayer(double[] input)
        {
            double[] hidden = new double[hiddenSize];
            for (int j = 0; j < hiddenSize; j++) { double sum = 0; for (int i = 0; i < inputSize; i++) sum += input[i] * weightsInputHidden[i, j]; hidden[j] = Math.Tanh(sum); }
            return hidden;
        }
        private double GetOutput(double[] hidden)
        {
            double sum = 0; for (int i = 0; i < hiddenSize; i++) sum += hidden[i] * weightsHiddenOutput[i];
            return 1.0 / (1.0 + Math.Exp(-sum));
        }
    }
}