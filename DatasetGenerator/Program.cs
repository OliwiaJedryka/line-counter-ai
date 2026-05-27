using System;
using System.IO;

class Program
{
    private const int Width = 160;
    private const int Height = 120;
    private const int TotalImages = 1000; 
    private const string OutputFolder = "Dane_Treningowe";
    private const string CsvFile = "dataset.csv";

    static void Main(string[] args)
    {
        Random rand = new Random();
        
        if (Directory.Exists(OutputFolder)) Directory.Delete(OutputFolder, true);
        Directory.CreateDirectory(OutputFolder);

        using var csvWriter = new StreamWriter(Path.Combine(OutputFolder, CsvFile));
        csvWriter.WriteLine("filename,line_count");

        Console.WriteLine($"Rozpoczynam generowanie {TotalImages} obrazów (Bresenham Engine)...");

        for (int i = 0; i < TotalImages; i++)
        {
            int lineCount = rand.Next(0, 9);
            
            // Tworzymy czarną matrycę (0 - czarny, 255 - biały)
            byte[,] canvas = new byte[Width, Height];

            // 1. Rysowanie linii algorytmem Bresenhama
            for (int l = 0; l < lineCount; l++)
            {
                int x0 = rand.Next(Width);
                int y0 = rand.Next(Height);
                int x1 = rand.Next(Width);
                int y1 = rand.Next(Height);

                DrawBresenhamLine(canvas, x0, y0, x1, y1);
            }

            // 2. Nakładanie szumu "sól i pieprz" bezpośrednio na matrycę
            AddSaltAndPepper(canvas, rand, density: 0.03f);

            // 3. Zapis do pliku BMP
            string fileName = $"img_{i:D4}.bmp"; // Zapisujemy jako BMP
            string filePath = Path.Combine(OutputFolder, fileName);
            SaveBitmap(canvas, filePath);

            csvWriter.WriteLine($"{fileName},{lineCount}");

            if (i % 100 == 0) Console.WriteLine($"Wygenerowano {i}/{TotalImages}...");
        }

        Console.WriteLine($"Sukces! Dane zapisano w folderze: {OutputFolder}");
    }

    // Klasyczny, niezawodny algorytm komputerowy do rysowania linii między punktami
    private static void DrawBresenhamLine(byte[,] canvas, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            if (x0 >= 0 && x0 < Width && y0 >= 0 && y0 < Height)
                canvas[x0, y0] = 255; // Rysuj biały piksel

            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private static void AddSaltAndPepper(byte[,] canvas, Random rand, float density)
    {
        int totalPixels = Width * Height;
        int noiseCount = (int)(totalPixels * density);

        for (int i = 0; i < noiseCount; i++)
        {
            int x = rand.Next(Width);
            int y = rand.Next(Height);

            // 50% szans na sól (255), 50% na pieprz (0)
            canvas[x, y] = rand.Next(2) == 0 ? (byte)255 : (byte)0;
        }
    }

    // Czysto-bajtowy zapis macierzy do formatu pliku BMP (Monochromatyczny 8-bit)
    private static void SaveBitmap(byte[,] canvas, string path)
    {
        using var stream = new FileStream(path, FileMode.Create);
        using var writer = new BinaryWriter(stream);

        // Szerokość w bajtach musi być wielokrotnością 4 (BMP Padding)
        int paddedWidth = (Width + 3) & ~3;
        int totalImageData = paddedWidth * Height;

        // Nagłówek pliku (Bitmap File Header)
        writer.Write((ushort)0x4D42); // "BM"
        writer.Write(14 + 40 + 1024 + totalImageData); // Rozmiar pliku
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(14 + 40 + 1024); // Offset do danych pikseli

        // Nagłówek obrazu (Bitmap Info Header)
        writer.Write(40); // Rozmiar nagłówka info
        writer.Write(Width);
        writer.Write(Height); // Dodatnia wartość = rysowanie od dołu do góry
        writer.Write((ushort)1); // Płaszczyzny
        writer.Write((ushort)8); // 8-bitów na piksel (skala szarości)
        writer.Write(0); // Kompresja (brak)
        writer.Write(totalImageData);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        // Paleta kolorów (dla obrazu 8-bit musimy zdefiniować 256 odcieni szarości)
        for (int i = 0; i < 256; i++)
        {
            writer.Write((byte)i); // B
            writer.Write((byte)i); // G
            writer.Write((byte)i); // R
            writer.Write((byte)0); // Zarezerwowane
        }

        // Zapis pikseli (od dolnego wiersza do górnego zgodnie ze specyfikacją BMP)
        byte[] rowBuffer = new byte[paddedWidth];
        for (int y = Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                rowBuffer[x] = canvas[x, y];
            }
            writer.Write(rowBuffer);
        }
    }
}