using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        List<List<Tile>> placedTiles;
        int numLayers = 2;

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

            placedTiles = new List<List<Tile>>(numLayers);

            // Initialize the inner lists
            for (int i = 0; i < numLayers; i++)
                placedTiles.Add(new List<Tile>(levelWidthInTiles * levelHeightInTiles));

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

                    foreach (List<Tile> listOfTiles in placedTiles)
                        listOfTiles.Add(null);
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

                textBoxLeftHeight.Text = selectedTile.leftHeight.ToString();
                textBoxRightHeight.Text = selectedTile.rightHeight.ToString();
                buttonTopCollision.IsChecked = selectedTile.topCollision == true ? true : false;
                buttonRightCollision.IsChecked = selectedTile.rightCollision == true ? true : false;
                buttonBottomCollision.IsChecked = selectedTile.bottomCollision == true ? true : false;
                buttonLeftCollision.IsChecked = selectedTile.leftCollision == true ? true : false;
                buttonIsPole.IsChecked = selectedTile.isPole == true ? true : false;
                buttonIsEdge.IsChecked = selectedTile.isEdge == true ? true : false;
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
                placedTiles[selectedTile.layer][buttonIndex] = null;
            } else {
                // Assign tile
                tilePlacerButton.Background = selectedButton.Background;
                placedTiles[selectedTile.layer][buttonIndex] = selectedTile;
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

            // Merge all layers into new object. This guarantees only one tile for each "spot" regardless of layer.
            List<Tile> finalPlacedTiles = new List<Tile>(Enumerable.Repeat<Tile>(null, levelWidthInTiles * levelHeightInTiles));
            for (int i = 0; i < numLayers; i++) {
                for (int j = 0; j < placedTiles[i].Count(); j++) {
                    if (placedTiles[i][j] != null)
                        finalPlacedTiles[j] = placedTiles[i][j];
                }
            }

            // Calculate number of non-blank tiles
            int tileCount = finalPlacedTiles.Count(t => t != null);
            if (tileCount < 1) return;

            using (StreamWriter sw = new StreamWriter(savePath)) {
                // File format:
                // First line is # tiles wide, # tiles tall, # tiles (non-blank) per layer
                // Second line is src file name
                // After that, one line per tile
                // Each line is src x coord, src y coord, leftHeight, rightHeight, then 1 or 0 for collision top, right, bottom, left 
                // -1 indicates blank tile
                string firstLine = levelWidthInTiles + " " + levelHeightInTiles + " ";
                for (int i = 0; i < numLayers; i++)
                    firstLine += placedTiles[i].Count(t => t != null) + " ";
                sw.WriteLine(firstLine);
                sw.WriteLine(loadImageSrcLabel.Content);

                for (int i = 0; i < finalPlacedTiles.Count; i++) {
                    int nullCount = 0;
                    while (i < finalPlacedTiles.Count && finalPlacedTiles[i] == null) {
                        nullCount++;
                        i++;
                    }

                    if (nullCount != 0) {
                        sw.WriteLine("-" + nullCount);
                        nullCount = 0;

                        if (i >= finalPlacedTiles.Count) break;
                    }

                    Tile tile = finalPlacedTiles[i];

                    // Determine mutex property value
                    int mutexProperty = 0;
                    if (tile.isPole)
                        mutexProperty = 1;

                    sw.WriteLine(
                        (tileWidth * tile.x) + " " +
                        (tileHeight * tile.y) + " " +
                        tile.leftHeight + " " +
                        tile.rightHeight + " " +
                        Convert.ToInt32(tile.topCollision) + " " +
                        Convert.ToInt32(tile.rightCollision) + " " +
                        Convert.ToInt32(tile.bottomCollision) + " " +
                        Convert.ToInt32(tile.leftCollision) + " " +
                        Convert.ToInt32(tile.isEdge) + " " +
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

                    if (splitLine[0].StartsWith("-")) {
                        int skipValue = int.Parse(splitLine[0].Split('-').Last());
                        count += skipValue;
                        continue;
                    }

                    // Find matching source tile
                    Tile srcTile = srcTiles.SingleOrDefault(t => t.x * tileWidth == int.Parse(splitLine[0]) && t.y * tileHeight == int.Parse(splitLine[1]));
                    if (srcTile != null) {
                        int layer = int.Parse(splitLine[10].ToString());

                        Button button = (Button)FindName("levelTile" + count);
                        button.Background = new ImageBrush(srcTile.image.Source);

                        if (splitLine.Count() != 11) continue;

                        srcTile.leftHeight = int.Parse(splitLine[2]);
                        srcTile.rightHeight = int.Parse(splitLine[3]);
                        srcTile.topCollision = splitLine[4].ToString() == "1" ? true : false;
                        srcTile.rightCollision = splitLine[5].ToString() == "1" ? true : false;
                        srcTile.bottomCollision = splitLine[6].ToString() == "1" ? true : false;
                        srcTile.leftCollision = splitLine[7].ToString() == "1" ? true : false;
                        srcTile.isEdge = splitLine[8].ToString() == "1" ? true : false;
                        srcTile.layer = layer;

                        // Mutex properties will default to false, so only need to think about setting them to true
                        string mutexProperty = splitLine[9].ToString();
                        if (mutexProperty == "1")
                            srcTile.isPole = true;


                        placedTiles[layer][count] = srcTile;
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
            if (textBoxLeftHeight.IsFocused || textBoxRightHeight.IsFocused) return;

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

        private void TileHeight_KeyUp(object sender, KeyEventArgs e) {
            if (selectedTile == null) return;

            int height;

            if (int.TryParse(textBoxLeftHeight.Text, out height))
                selectedTile.leftHeight = height;

            if (int.TryParse(textBoxRightHeight.Text, out height))
                selectedTile.rightHeight = height;
        }

        private void SetLayerLabel(string key) {
            selectedTile.layer = int.Parse(key);
            labelLayer.Content = key;
        }

        private void buttonIsEdge_Click(object sender, RoutedEventArgs e) {
            if (selectedTile == null) return;

            selectedTile.isEdge = selectedTile.isEdge == true ? false : true;
            buttonIsEdge.IsChecked = selectedTile.isEdge == true ? true : false;
        }
    }
}
