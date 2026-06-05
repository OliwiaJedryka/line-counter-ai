using System;
using System.IO;

namespace LineCounterApp
{
    public class ImageProcessor
    {
        public int Width { get; private set; } = 160;
        public int Height { get; private set; } = 120;

        public byte[,] LoadBmpToMatrix(string filePath)
        {
            byte[,] matrix = new byte[Width, Height];
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            stream.Seek(14 + 40 + 1024, SeekOrigin.Begin);

            int paddedWidth = (Width + 3) & ~3;
            byte[] rowBuffer = new byte[paddedWidth];

            for (int y = Height - 1; y >= 0; y--)
            {
                reader.Read(rowBuffer, 0, paddedWidth);
                for (int x = 0; x < Width; x++)
                {
                    matrix[x, y] = rowBuffer[x];
                }
            }

            return matrix;
        }

        public byte[,] ApplyMedianFilter(byte[,] inputMatrix)
        {
            byte[,] tempMatrix = new byte[Width, Height];
            byte[,] outputMatrix = new byte[Width, Height];
            byte[] window = new byte[25];

            // FAZA 1: Usunięcie gęstego szumu sól i pieprz (Okno 5x5)
            for (int x = 2; x < Width - 2; x++)
            {
                for (int y = 2; y < Height - 2; y++)
                {
                    int k = 0;
                    for (int fx = -2; fx <= 2; fx++)
                    {
                        for (int fy = -2; fy <= 2; fy++)
                        {
                            window[k++] = inputMatrix[x + fx, y + fy];
                        }
                    }

                    Array.Sort(window);
                    tempMatrix[x, y] = (byte)Math.Min(inputMatrix[x, y], window[20]);
                }
            }

            // FAZA 2: Filtr spójności obszaru (Usuwanie krótkich kresek/artefaktów)
            // Sprawdzamy okno 9x9 wokół każdego piksela, który przetrwał pierwszą fazę
            for (int x = 4; x < Width - 4; x++)
            {
                for (int y = 4; y < Height - 4; y++)
                {
                    if (tempMatrix[x, y] > 0)
                    {
                        int totalPixelsInArea = 0;

                        // Zliczamy ile w ogóle jasnych pikseli jest w okolicy 9x9
                        for (int fx = -4; fx <= 4; fx++)
                        {
                            for (int fy = -4; fy <= 4; fy++)
                            {
                                if (tempMatrix[x + fx, y + fy] > 0)
                                {
                                    totalPixelsInArea++;
                                }
                            }
                        }

                        // Jeśli cała grupa ma łącznie mniej niż 5 pikseli, to jest to szum tła.
                        // Prawdziwa linia w oknie 9x9 zostawi znacznie więcej punktów.
                        if (totalPixelsInArea >= 5)
                        {
                            outputMatrix[x, y] = tempMatrix[x, y];
                        }
                        else
                        {
                            outputMatrix[x, y] = 0; // Skasuj krótką kreskę-widmo
                        }
                    }
                }
            }

            return outputMatrix;
        }
    }
}