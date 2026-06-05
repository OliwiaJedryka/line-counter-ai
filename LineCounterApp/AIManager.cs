using System;
using System.Collections.Generic;
using System.IO;

namespace LineCounterApp
{
    public class AIManager
    {
        // 1. Zmniejszono sieć do 16 neuronów w warstwie ukrytej (zamiast 32)
        private NeuralNetwork net = new NeuralNetwork(160 * 120, 16);
        public ImageProcessor Processor { get; private set; } = new ImageProcessor();
        
        // 2. Wymuszenie ścieżki pliku w folderze aplikacji
        private readonly string weightPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LineCounterAI.weights");

        public AIManager() 
        { 
            // Przy starcie automatycznie wczytuje wagi, jeśli istnieją
            net.LoadWeights(weightPath); 
        }

        public void LoadAndTrain(string csvPath, Action<string> logger)
{
    List<double[]> inputs = new List<double[]>();
    List<double> targets = new List<double>();

    if (!File.Exists(csvPath))
    {
        logger("BŁĄD: Plik CSV nie istnieje!");
        return;
    }

    // Pobieramy ścieżkę do folderu, w którym leży plik CSV
    string csvDirectory = Path.GetDirectoryName(csvPath);

    var lines = File.ReadAllLines(csvPath);
    int lineNumber = 0;
    int skippedCount = 0;

    foreach (var line in lines)
    {
        lineNumber++;
        if (string.IsNullOrWhiteSpace(line)) continue;

        var parts = line.Split(';');
        
        // Sprawdzenie separatora (jeśli nie ma średnika, może jest przecinek?)
        if (parts.Length < 2)
        {
            parts = line.Split(',');
            if (parts.Length < 2)
            {
                skippedCount++;
                continue;
            }
        }

        string imgPath = parts[0].Trim();

        // NAPRAWA ŚCIEŻKI: Jeśli ścieżka jest względna, połącz ją z folderem pliku CSV
        if (!Path.IsPathRooted(imgPath))
        {
            imgPath = Path.Combine(csvDirectory, imgPath);
        }

        // Sprawdzenie czy plik faktycznie istnieje pod wyliczoną ścieżką
        if (!File.Exists(imgPath))
        {
            skippedCount++;
            continue; // Jeśli nie istnieje, pomija
        }

        if (!double.TryParse(parts[1], out double targetCount))
        {
            skippedCount++;
            continue;
        }

        byte[,] raw = Processor.LoadBmpToMatrix(imgPath);
        byte[,] filtered = Processor.ApplyMedianFilter(raw);
        
        filtered = AddRandomNoise(filtered); 
        
        inputs.Add(Flatten(filtered));
        targets.Add(targetCount / 10.0);
    }

    // Jeśli pętla nic nie dodała, powiadom użytkownika, ile linii zostało odrzuconych
    if (inputs.Count == 0)
    {
        logger($"BŁĄD: Odrzucono wszystkie wiersze ({skippedCount} sztuk). Sprawdź ścieżki w CSV!");
        return;
    }

    logger($"Rozpoczęto trenowanie na {inputs.Count} próbkach (Pominięto: {skippedCount})...");
    
    net.Train(inputs, targets, 500, 0.05, logger);
    
    try {
        net.SaveWeights(weightPath);
        logger("Sukces: Wagi zapisane w: " + weightPath);
    } catch (Exception ex) {
        logger("BŁĄD ZAPISU WAG: " + ex.Message);
    }
    
    logger("Trening zakończony i zapisany!");
}

        private byte[,] AddRandomNoise(byte[,] matrix)
        {
            Random rand = new Random();
            for (int i = 0; i < 300; i++) // 300 losowych punktów szumu
            {
                int x = rand.Next(160);
                int y = rand.Next(120);
                matrix[x, y] = (byte)(rand.Next(2) * 255);
            }
            return matrix;
        }

        public int PredictLines(byte[,] filteredMatrix, out double confidence)
        {
            double prediction = net.Forward(Flatten(filteredMatrix));
            int count = (int)Math.Round(prediction * 10.0);
            confidence = (1.0 - Math.Abs(prediction - (count / 10.0))) * 100.0;
            return count;
        }

        private double[] Flatten(byte[,] matrix)
        {
            double[] flat = new double[160 * 120];
            for (int y = 0; y < 120; y++)
                for (int x = 0; x < 160; x++)
                    flat[y * 160 + x] = matrix[x, y] / 255.0; // Normalizacja 0-1
            return flat;
        }
    }
}