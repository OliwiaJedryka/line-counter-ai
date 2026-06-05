using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace LineCounterApp
{
    public class MainWindow : Window
    {
        private readonly AIManager _aiManager = new();
        private readonly ImageProcessor _processor = new();
        private TextBlock _statusLabel = new();
        private TextBlock _resultLabel = new();
        private TextBlock _confidenceLabel = new();
        private Image _originalImageDisplay = new();
        private Image _filteredImageDisplay = new();

        [STAThread]
        public static void Main() { Application app = new Application(); app.Run(new MainWindow()); }

        public MainWindow()
        {
            Title = "Detektor AI"; Width = 750; Height = 500;
            BuildUI();
        }

        private void BuildUI()
{
    var mainGrid = new Grid { Margin = new Thickness(10) };
    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Przyciski
    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Obrazy
    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Wyniki

    // Panel górny
    var topPanel = new StackPanel { Orientation = Orientation.Horizontal };
    var btnLoad = new Button { Content = "Wczytaj Obraz", Margin = new Thickness(0,0,10,0), Padding = new Thickness(5) };
    btnLoad.Click += LoadImage_Click; // Pamiętaj o obsłudze tego zdarzenia!
    var btnTrain = new Button { Content = "Trenuj AI", Background = Brushes.Green, Foreground = Brushes.White, Padding = new Thickness(5) };
    btnTrain.Click += Train_Click;
    _statusLabel = new TextBlock { Text = "Gotowy", Margin = new Thickness(15, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    
    topPanel.Children.Add(btnLoad);
    topPanel.Children.Add(btnTrain);
    topPanel.Children.Add(_statusLabel);
    Grid.SetRow(topPanel, 0);

    // Panel środkowy (obrazy)
    var imageGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 10, 0, 10) };
    imageGrid.Children.Add(new GroupBox { Header = "Oryginał", Content = _originalImageDisplay });
    imageGrid.Children.Add(new GroupBox { Header = "Po filtracji", Content = _filteredImageDisplay });
    Grid.SetRow(imageGrid, 1);

    // Panel dolny (wynik)
    var bottomPanel = new StackPanel();
    bottomPanel.Children.Add(_resultLabel = new TextBlock { Text = "Wynik: -", FontSize = 20, FontWeight = FontWeights.Bold });
    bottomPanel.Children.Add(_confidenceLabel = new TextBlock { Text = "Pewność: 0%" });
    Grid.SetRow(bottomPanel, 2);

    // Dodanie wszystkiego do głównego grida
    mainGrid.Children.Add(topPanel);
    mainGrid.Children.Add(imageGrid);
    mainGrid.Children.Add(bottomPanel);
    
    Content = mainGrid;
}

        private async void Train_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
            if (dlg.ShowDialog() == true)
            {
                // To jest ten "logger", który przesyła tekst do _statusLabel
                Action<string> logger = (msg) => Dispatcher.Invoke(() => _statusLabel.Text = msg);
                
                try {
                    await Task.Run(() => _aiManager.LoadAndTrain(dlg.FileName, logger));
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }
        // ... metody LoadImage_Click i inne ...

        private void LoadImage_Click(object sender, RoutedEventArgs e)
{
    OpenFileDialog dlg = new OpenFileDialog { Filter = "BMP files (*.bmp)|*.bmp" };
    if (dlg.ShowDialog() == true)
    {
        // 1. Wczytaj i przefiltruj obraz
        byte[,] raw = _processor.LoadBmpToMatrix(dlg.FileName);
        byte[,] filtered = _processor.ApplyMedianFilter(raw);
        
        // 2. Wyświetl obraz po filtracji
        _filteredImageDisplay.Source = ConvertMatrixToBitmap(filtered);
        _originalImageDisplay.Source = new BitmapImage(new Uri(dlg.FileName)); // Wyświetl oryginał

        // 3. Użyj AI do predykcji
        double confidence;
        int count = _aiManager.PredictLines(filtered, out confidence);
        
        // 4. Zaktualizuj UI
        _resultLabel.Text = $"Wykryto linii: {count}";
        _confidenceLabel.Text = $"Pewność: {confidence:F1}%";
    }
}

private WriteableBitmap ConvertMatrixToBitmap(byte[,] matrix)
{
    int width = 160; int height = 120;
    WriteableBitmap bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
    byte[] pixels = new byte[width * height];
    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            pixels[y * width + x] = matrix[x, y];
    bmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, width, 0);
    return bmp;
}
    }
}