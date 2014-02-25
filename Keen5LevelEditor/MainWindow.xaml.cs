using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Keen5LevelEditor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        int tileWidth;
        int tileHeight;
        int levelWidthInTiles;
        int levelHeightInTiles;

        Tile selectedTile;
        Button selectedButton;

        BitmapImage src;
        List<Tile> srcTiles;
        List<Tile> placedTiles;

        string savePath;

        public MainWindow() {
            InitializeComponent();

            tileWidth = 32;
            tileHeight = 32;
        }

        private void loadFile_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.OpenFileDialog open_dialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = open_dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            loadFileLabel.Content = open_dialog.FileName;

            src = new BitmapImage();
            src.BeginInit();
            src.UriSource = new Uri(open_dialog.FileName);
            src.CacheOption = BitmapCacheOption.OnLoad;
            src.EndInit();

            Console.WriteLine("File opened.");

            srcTiles = new List<Tile>();

            // Create a table of tiles to choose from
            int tilesWide = src.PixelWidth / tileWidth;
            int tilesTall = src.PixelHeight / tileHeight;

            for (int i=0; i<tilesTall; i++) {
                // Add "row"
                StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

                for (int j=0; j<tilesWide; j++) {
                    // Create image
                    CroppedBitmap crop = new CroppedBitmap(src, new Int32Rect(j*tileWidth, i*tileHeight, tileWidth, tileHeight));
                    Tile tile = new Tile(crop, j, i);
                    srcTiles.Add(tile);

                    // Create button with image as background
                    string name = "tile" + (srcTiles.Count-1).ToString();
                    Button button = new Button() { Width = tileWidth, Height = tileHeight, Name = name };
                    ImageBrush brush = new ImageBrush();
                    brush.ImageSource = tile.image.Source;
                    button.Background = brush;
                    button.Click += tileSelector_Click;

                    // Add button to stackpanel
                    stackPanel.Children.Add(button);
                }

                TileList.Children.Add(stackPanel);
            }

            // Create a table of tiles for the level editing
            try {
                levelWidthInTiles = Convert.ToInt32(textboxLevelWidth.ToString());
                levelHeightInTiles = Convert.ToInt32(textboxLevelHeight.ToString());
            } catch (FormatException) {
                levelWidthInTiles = 16;
                levelHeightInTiles = 32;
            }

            placedTiles = new List<Tile>(levelHeightInTiles * levelWidthInTiles);
            int tileCount = 0;

            for (int i=0; i<levelHeightInTiles; i++) {
                // Add "row"
                StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

                for (int j=0; j<levelWidthInTiles; j++) {
                    // Create button
                    string name = "levelTile" +  tileCount.ToString();
                    Button button = new Button() { Width = tileWidth, Height = tileHeight, Name = name };
                    button.Click += tilePlacer_Click;

                    // Add button to stackpanel
                    stackPanel.Children.Add(button);

                    placedTiles.Add(null);
                    tileCount++;
                }

                Body.Children.Add(stackPanel);
            }
        }

        private void tileSelector_Click(object sender, RoutedEventArgs e) {
            Button tileSelectorButton = (Button)sender;

            // Find tile associated with button
            int tileIndex = Convert.ToInt32(tileSelectorButton.Name.Split(new [] {"tile"}, StringSplitOptions.None)[1]);
            Tile clickedTile = srcTiles[tileIndex];
            
            // Toggle selection of tile and button
            if (selectedTile == null || selectedTile != clickedTile) {
                if (selectedButton != null) selectedButton.BorderBrush = Brushes.Green;
                selectedButton = tileSelectorButton;
                tileSelectorButton.BorderBrush = Brushes.Red;

                selectedTile = clickedTile;
            } else {
                // Deselect tile
                selectedTile = null;
                selectedButton.BorderBrush = Brushes.Green;
                selectedButton = null;
            }
        }

        private void tilePlacer_Click(object sender, RoutedEventArgs e) {
            if (selectedTile == null) return;

            // Show image in button
            Button tilePlacerButton = (Button)sender;
            int buttonIndex = Convert.ToInt32(tilePlacerButton.Name.Split(new [] { "levelTile" }, StringSplitOptions.None)[1]);

            if (tilePlacerButton.Background != null && tilePlacerButton.Background == selectedButton.Background) {
                // Clear tile
                tilePlacerButton.ClearValue(Button.BackgroundProperty);
                placedTiles[buttonIndex] = null;
            } else {
                // Assign tile
                tilePlacerButton.Background = selectedButton.Background;
                placedTiles[buttonIndex] = selectedTile;
            }
        }

        private void saveFile_Click(object sender, RoutedEventArgs e) {
            if (savePath == null) {
                System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog();
                System.Windows.Forms.DialogResult result = saveDialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                    return;

                savePath = saveDialog.FileName;
            }

            if (placedTiles == null || placedTiles.Count == 0) return;

            using (StreamWriter sw = new StreamWriter(savePath)) {
                // File format:
                // First line is # tiles wide and # tiles tall
                // After that, one line per tile
                // Each line is src x coord, src y coord, or -1 to indicate blank tile
                sw.WriteLine(levelWidthInTiles.ToString() + " " + levelHeightInTiles.ToString());

                foreach (Tile tile in placedTiles) {
                    if (tile == null) {
                        sw.WriteLine("-1");
                        continue;
                    }

                    sw.WriteLine((tileWidth * tile.x).ToString() + " " + (tileHeight * tile.y).ToString());
                }
            }
        }
    }
}
