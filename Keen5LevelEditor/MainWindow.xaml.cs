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

                buttonTopCollision.IsChecked = selectedTile.topCollision == true ? true : false;
                buttonRightCollision.IsChecked = selectedTile.rightCollision == true ? true : false;
                buttonBottomCollision.IsChecked = selectedTile.bottomCollision == true ? true : false;
                buttonLeftCollision.IsChecked = selectedTile.leftCollision == true ? true : false;
                buttonIsPole.IsChecked = selectedTile.isPole == true ? true : false;
                labelLayer.Content = selectedTile.layer;
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
                // Each line is src x coord, src y coord, then 1 or 0 for collision top, right, bottom, left 
                // -1 indicates blank tile
                sw.WriteLine(levelWidthInTiles + " " + levelHeightInTiles + " " + tileCount);
                sw.WriteLine(loadImageSrcLabel.Content);

                foreach (Tile tile in placedTiles) {
                    if (tile == null) {
                        sw.WriteLine("-1");
                        continue;
                    }

                    // Determine mutex property value
                    int mutexProperty = 0;
                    if (tile.isPole)
                        mutexProperty = 1;

                    sw.WriteLine(
                        (tileWidth * tile.x) + " " +
                        (tileHeight * tile.y) + " " +
                        Convert.ToInt32(tile.topCollision) + " " +
                        Convert.ToInt32(tile.rightCollision) + " " +
                        Convert.ToInt32(tile.bottomCollision) + " " +
                        Convert.ToInt32(tile.leftCollision) + " " +
                        mutexProperty + " " +
                        tile.layer
                    );
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

                            if (splitLine.Count() != 8) continue;

                            placedTiles[count].topCollision = splitLine[2].ToString() == "1" ? true : false;
                            placedTiles[count].rightCollision = splitLine[3].ToString() == "1" ? true : false;
                            placedTiles[count].bottomCollision = splitLine[4].ToString() == "1" ? true : false;
                            placedTiles[count].leftCollision = splitLine[5].ToString() == "1" ? true : false;
                            placedTiles[count].layer = int.Parse(splitLine[7].ToString());

                            // Mutex properties will default to false, so only need to think about setting them to true
                            string mutexProperty = splitLine[6].ToString();
                            if (mutexProperty == "1")
                                placedTiles[count].isPole = true;
                        }
                    }

                    count++;
                }
            }

            Console.WriteLine("File loaded.");
        }

        private void propertyButton_Click(object sender, RoutedEventArgs e) {
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

        private void mutexPropertyButton_Click(object sender, RoutedEventArgs e) {
            if (selectedTile == null) return;

            ToggleButton clicked = (ToggleButton)sender;

            switch (clicked.Name) {
                case "buttonIsPole":
                    selectedTile.isPole = selectedTile.isPole == true ? false : true;
                    buttonIsPole.IsChecked = selectedTile.isPole == true ? true : false;

                    // Reset other mutex properties
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
                case Key.D0:
                    SetLayerLabel("0");
                    break;
                case Key.D1:
                    SetLayerLabel("1");
                    break;
                case Key.D2:
                    SetLayerLabel("2");
                    break;
                case Key.D3:
                    SetLayerLabel("3");
                    break;
                case Key.D4:
                    SetLayerLabel("4");
                    break;
                case Key.D5:
                    SetLayerLabel("5");
                    break;
                case Key.D6:
                    SetLayerLabel("6");
                    break;
                case Key.D7:
                    SetLayerLabel("7");
                    break;
                case Key.D8:
                    SetLayerLabel("8");
                    break;
                case Key.D9:
                    SetLayerLabel("9");
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

        private void SetLayerLabel(string key) {
            selectedTile.layer = int.Parse(key);
            labelLayer.Content = key;
        }

        private void ToggleIsPole() {
            if (selectedTile == null) return;

            selectedTile.isPole = selectedTile.isPole == true ? false : true;
            buttonIsPole.IsChecked = selectedTile.isPole == true ? true : false;
        }
    }
}
