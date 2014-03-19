using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        }

        private void setImageSource(string filename) {
            src = new BitmapImage();
            src.BeginInit();
            src.UriSource = new Uri(filename);
            src.CacheOption = BitmapCacheOption.OnLoad;
            src.EndInit();

            loadImageSrcLabel.Content = filename;

            Console.WriteLine("File opened.");
        }

        private void createTables() {
            srcTiles = new List<Tile>();

            tileWidth = Convert.ToInt32(textboxTileWidth.Text);
            tileHeight = Convert.ToInt32(textboxTileHeight.Text);

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
                levelWidthInTiles = Convert.ToInt32(textboxLevelWidth.Text);
                levelHeightInTiles = Convert.ToInt32(textboxLevelHeight.Text);
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
                    this.RegisterName(name, button);

                    // Add button to stackpanel
                    stackPanel.Children.Add(button);

                    placedTiles.Add(null);
                    tileCount++;
                }

                Body.Children.Add(stackPanel);
            }
        }

        private void loadImageSrc_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.OpenFileDialog open_dialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = open_dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            setImageSource(open_dialog.FileName);
            createTables();
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

            // Calculate number of non-blank tiles
            int tileCount = placedTiles.Where(t => t != null).Count();
            if (tileCount < 1) return;

            using (StreamWriter sw = new StreamWriter(savePath)) {
                // File format:
                // First line is # tiles wide, # tiles tall, # tiles (non-blank) total
                // Second line is src file name
                // After that, one line per tile
                // Each line is src x coord, src y coord, or -1 to indicate blank tile
                sw.WriteLine(levelWidthInTiles.ToString() + " " + levelHeightInTiles.ToString() + " " + tileCount.ToString());
                sw.WriteLine(loadImageSrcLabel.Content);

                foreach (Tile tile in placedTiles) {
                    if (tile == null) {
                        sw.WriteLine("-1");
                        continue;
                    }

                    sw.WriteLine((tileWidth * tile.x).ToString() + " " + (tileHeight * tile.y).ToString());
                }
            }

            Console.WriteLine("File saved.");
        }

        private void loadSave_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.OpenFileDialog open_dialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = open_dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            string line;

            using (StreamReader sr = new StreamReader(open_dialog.FileName)) {
                // Exception: Get first two lines differently
                // Line 1
                line = sr.ReadLine();
                string[] line1Values = line.Split(' ');
                levelWidthInTiles = Convert.ToInt32(line1Values[0]);
                levelHeightInTiles = Convert.ToInt32(line1Values[1]);

                textboxLevelWidth.Text = levelWidthInTiles.ToString();
                textboxLevelHeight.Text = levelHeightInTiles.ToString();

                // Line 2
                line = sr.ReadLine();
                Console.WriteLine(line);
                setImageSource(line);
                createTables();

                int count = 0;

                while ((line = sr.ReadLine()) != null) {
                    string[] splitLine = line.Split(' ');

                    if (splitLine[0] == "-1") {
                        count++;
                        continue;
                    }

                    for (int i = 0; i < srcTiles.Count(); i++) {
                        if (srcTiles[i].x * tileWidth == Convert.ToInt32(splitLine[0]) && srcTiles[i].y * tileHeight == Convert.ToInt32(splitLine[1])) {
                            Button button = (Button)FindName("levelTile" + count);
                            button.Background = new ImageBrush(srcTiles[i].image.Source);
                            placedTiles[count] = srcTiles[i];
                        }

                        Console.WriteLine(srcTiles[i].x + "," + srcTiles[i].y);
                    }

                    count++;
                }
            }

            Console.WriteLine("File loaded.");
        }

        private void collisionButton_Click(object sender, RoutedEventArgs e) {
            if (selectedTile == null) return;

            ToggleButton clicked = (ToggleButton)sender;

            switch (clicked.Name) {
                case "buttonTopCollision":
                    ToggleTopCollision();
                    break;
                case "buttonRightCollision":
                    ToggleRightCollision();
                    break;
                case "buttonBottomCollision":
                    ToggleBottomCollision();
                    break;
                case "buttonLeftCollision":
                    ToggleLeftCollision();
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e) {
            if (selectedTile == null) return;

            switch (e.Key) {
                case Key.NumPad8:
                    ToggleTopCollision();
                    break;
                case Key.NumPad6:
                    ToggleRightCollision();
                    break;
                case Key.NumPad2:
                    ToggleBottomCollision();
                    break;
                case Key.NumPad4:
                    ToggleLeftCollision();
                    break;
            }
        }

        private void ToggleTopCollision() {
            if (selectedTile == null) return;

            selectedTile.topCollision = selectedTile.topCollision == true ? false : true;
            buttonTopCollision.IsChecked = selectedTile.topCollision == true ? true : false;
        }

        private void ToggleRightCollision() {
            if (selectedTile == null) return;

            selectedTile.rightCollision = selectedTile.rightCollision == true ? false : true;
            buttonRightCollision.IsChecked = selectedTile.rightCollision == true ? true : false;
        }

        private void ToggleBottomCollision() {
            if (selectedTile == null) return;

            selectedTile.bottomCollision = selectedTile.bottomCollision == true ? false : true;
            buttonBottomCollision.IsChecked = selectedTile.bottomCollision == true ? true : false;
        }

        private void ToggleLeftCollision() {
            if (selectedTile == null) return;

            selectedTile.leftCollision = selectedTile.leftCollision == true ? false : true;
            buttonLeftCollision.IsChecked = selectedTile.leftCollision == true ? true : false;
        }
    }
}
