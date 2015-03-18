using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Keen5LevelEditor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        int tileWidth;
        int tileHeight;
        int levelWidthInTiles;
        int levelHeightInTiles;
        int numLayers;

        Tile selectedTile;
        LocationData selectedLocation;
        Button selectedButton;

        List<List<Button>> gameboardButtons = new List<List<Button>>();
        Button selectedGameboardButton = null;
        int? selectedGameboardButtonIndex = null;

        BitmapImage src;
        List<Tile> srcTiles;
        List<List<Tile>> placedTiles;
        List<LocationData> locations;
        List<MovingPlatform> platforms = new List<MovingPlatform>();
        MovingPlatform selectedPlatform = null;

        string savePath;

        bool isLevelLoaded = false;

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
            numLayers = int.Parse(textboxNumLayers.Text);

            if (numLayers < 1)
                numLayers = 1;

            // Populate dropdown list of layers
            for (int i = 0; i < numLayers; i++) {
                layerSelector.Items.Add(new ComboBoxItem { Name = "selectLayer" + i, Content = "Layer " + i });
            }
            layerSelector.SelectedIndex = 0;

            // Create a table of tiles to choose from
            int tilesWide = src.PixelWidth / tileWidth;
            int tilesTall = src.PixelHeight / tileHeight;

            for (int i = 0; i < tilesTall; i++) {
                // Add "row"
                StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

                for (int j = 0; j < tilesWide; j++) {
                    // Create image
                    CroppedBitmap crop = new CroppedBitmap(src, new Int32Rect(j * tileWidth, i * tileHeight, tileWidth, tileHeight));
                    Tile tile = new Tile(crop, j, i);
                    srcTiles.Add(tile);

                    // Create button with image as background
                    string name = "tile" + (srcTiles.Count - 1).ToString();
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

            // Initialize Locations
            locations = new List<LocationData>(levelWidthInTiles * levelHeightInTiles);
            for (int i = 0; i < locations.Capacity; i++)
                locations.Add(new LocationData());

            // Initialize the inner lists
            for (int i = 0; i < numLayers; i++)
                placedTiles.Add(new List<Tile>(levelWidthInTiles * levelHeightInTiles));

            int tileCount = 0;

            for (int i = 0; i < numLayers; i++) {
                // Add list of buttons
                gameboardButtons.Add(new List<Button>(levelHeightInTiles * levelWidthInTiles));

                for (int j = 0; j < levelHeightInTiles; j++) {
                    // Add "row"
                    StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };

                    for (int k = 0; k < levelWidthInTiles; k++) {
                        // Create button
                        string name = "levelTile" + tileCount.ToString();
                        Button button = new Button() { Width = tileWidth, Height = tileHeight, Name = name, Foreground = Brushes.Red, Content = "" };
                        button.Click += tilePlacer_Click;
                        this.RegisterName(name, button);

                        // Add first layer of buttons to stackpanel
                        if (i == 0)
                            stackPanel.Children.Add(button);

                        // Save reference to button
                        gameboardButtons[i].Add(button);

                        foreach (List<Tile> listOfTiles in placedTiles)
                            listOfTiles.Add(null);
                        tileCount++;
                    }

                    Body.Children.Add(stackPanel);
                }
            }
        }

        private void loadImageSrc_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.OpenFileDialog open_dialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = open_dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            setImageSource(open_dialog.FileName);
            createTables();

            isLevelLoaded = true;
        }

        private void tileSelector_Click(object sender, RoutedEventArgs e) {
            Button tileSelectorButton = (Button)sender;

            // Find tile associated with button
            int tileIndex = Convert.ToInt32(tileSelectorButton.Name.Split(new[] { "tile" }, StringSplitOptions.None)[1]);
            Tile clickedTile = srcTiles[tileIndex];

            // Before changing tiles, save current tile's notes if they exist
            if (selectedTile != null && textBoxNotes.Text.Trim() != "") {
                selectedTile.notes = textBoxNotes.Text.Trim();
            }

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
                buttonIsPoleEdge.IsChecked = selectedTile.isPoleEdge == true ? true : false;
                buttonIsEdge.IsChecked = selectedTile.isEdge == true ? true : false;
                textBoxNotes.Text = selectedTile.notes;
            } else {
                // Deselect tile
                selectedTile = null;
                selectedButton.BorderBrush = Brushes.Green;
                selectedButton = null;
            }
        }

        private void tilePlacer_Click(object sender, RoutedEventArgs e) {
            selectedGameboardButton = (Button)sender;
            selectedGameboardButtonIndex = GetGameboardButtonIndex(selectedGameboardButton);
            selectedLocation = locations[selectedGameboardButtonIndex.Value];

            noUnit.IsChecked = selectedLocation.unit == UnitEnum.None;
            keen.IsChecked = selectedLocation.unit == UnitEnum.Keen;
            sparky.IsChecked = selectedLocation.unit == UnitEnum.Sparky;
            ampton.IsChecked = selectedLocation.unit == UnitEnum.Ampton;
            platform.IsChecked = selectedLocation.unit == UnitEnum.MovingPlatform;
            laser.IsChecked = selectedLocation.unit == UnitEnum.Laser;
            noItem.IsChecked = selectedLocation.item == ItemEnum.None;
            ammo.IsChecked = selectedLocation.item == ItemEnum.Ammo;
            gum.IsChecked = selectedLocation.item == ItemEnum.Gum;
            marshmellow.IsChecked = selectedLocation.item == ItemEnum.Marshmellow;
            vitalin.IsChecked = selectedLocation.item == ItemEnum.Vitalin;

            // If user switches off of place platform mode, hide it
            if (!radioPlacePlatformDest.IsChecked.HasValue || !radioPlacePlatformDest.IsChecked.Value) {
                radioPlacePlatformDest.Visibility = Visibility.Collapsed;
                selectedPlatform = null;
            }

            // If a platform is at this location, allow destination placements
            if (radioSelectTiles.IsChecked.HasValue && radioSelectTiles.IsChecked.Value && selectedLocation.unit == UnitEnum.MovingPlatform) {
                radioPlacePlatformDest.Visibility = Visibility.Visible;
                selectedPlatform = platforms.SingleOrDefault(p => p.buttonIndex == selectedGameboardButtonIndex);
            }

            if (radioPlacePlatformDest.IsChecked.HasValue && radioPlacePlatformDest.IsChecked.Value && selectedPlatform != null) {
                SetPlatformDestination(selectedPlatform, selectedGameboardButton, selectedGameboardButtonIndex.Value);
            }

            if (selectedTile == null || !radioPlaceTiles.IsChecked.Value) return;

            // Show image in button
            if (selectedGameboardButton.Background != null && selectedGameboardButton.Background == selectedButton.Background) {
                // Clear tile
                selectedGameboardButton.ClearValue(Button.BackgroundProperty);
                placedTiles[layerSelector.SelectedIndex][selectedGameboardButtonIndex.Value] = null;
            } else {
                // Assign tile
                selectedGameboardButton.Background = selectedButton.Background;
                placedTiles[layerSelector.SelectedIndex][selectedGameboardButtonIndex.Value] = selectedTile;
            }
        }

        private MovingPlatform GetOrCreatePlatform(int buttonIndex) {
            MovingPlatform platform = platforms.SingleOrDefault(p => p.buttonIndex == buttonIndex);

            if (platform != null)
                return platform;

            Tuple<int, int> platformCoord = GetCoordinatesFromButtonIndex(buttonIndex);

            return new MovingPlatform {
                buttonIndex = buttonIndex,
                startX = platformCoord.Item1,
                startY = platformCoord.Item2,
                color = MovingPlatformColorEnum.Pink,
                tileDests = new List<Tuple<int, int>>()
            };
        }

        private void SetPlatformDestination(MovingPlatform platform, Button button, int buttonIndex) {
            Tuple<int, int> coord = GetCoordinatesFromButtonIndex(buttonIndex);

            if (platform.tileDests.Contains(coord)) {
                platform.tileDests.Remove(coord);
                button.Content = "";
            } else {
                platform.tileDests.Add(coord);
                button.Content = "D" + selectedPlatform.tileDests.Count.ToString();
            }

        }

        private Tuple<int, int> GetCoordinatesFromButtonIndex(int buttonIndex) {
            int row = buttonIndex / levelWidthInTiles;
            int col = buttonIndex - row * levelWidthInTiles;

            int xCoord = col * tileHeight;
            int yCoord = row * tileHeight;

            return new Tuple<int, int>(xCoord, yCoord);
        }

        private int GetButtonIndexFromCoordinates(Tuple<int, int> coord) {
            int col = coord.Item1 / tileHeight;
            int row = coord.Item2 / tileHeight;

            return col + row * levelWidthInTiles;
        }

        private void unitPlacer_Click(object sender, RoutedEventArgs e) {
            if (selectedGameboardButton == null || !radioSelectTiles.IsChecked.Value) return;

            RadioButton unitSelector = (RadioButton)sender;

            selectedLocation.unit = (UnitEnum)Enum.Parse(typeof(UnitEnum), unitSelector.Content.ToString());

            // If switching from Moving Platform to anything else, remove the platform object from the list
            if (selectedGameboardButton.Content.ToString() == UnitEnum.MovingPlatform.ToString() && unitSelector.Content.ToString() != UnitEnum.MovingPlatform.ToString()) {
                selectedGameboardButtonIndex = GetGameboardButtonIndex(selectedGameboardButton);
                MovingPlatform platform = platforms.SingleOrDefault(p => p.buttonIndex == selectedGameboardButtonIndex);
                platforms.Remove(platform);
            }

            if (unitSelector.Content.ToString() != "None") {
                selectedGameboardButton.BorderBrush = Brushes.Green;
                selectedGameboardButton.Content = unitSelector.Content;

                if (unitSelector.Content.ToString() == UnitEnum.MovingPlatform.ToString()) {
                    // Create platform if it doesn't exist here already
                    selectedPlatform = GetOrCreatePlatform(GetGameboardButtonIndex(selectedGameboardButton));
                    platforms.Add(selectedPlatform);
                    radioPlacePlatformDest.Visibility = Visibility.Visible;
                }
            } else {
                selectedGameboardButton.Content = "";
            }
        }

        private void itemPlacer_Click(object sender, RoutedEventArgs e) {
            if (selectedGameboardButton == null) return;

            RadioButton itemSelector = (RadioButton)sender;

            selectedLocation.item = (ItemEnum)Enum.Parse(typeof(ItemEnum), itemSelector.Content.ToString());

            if (itemSelector.Content.ToString() != "None") {
                selectedGameboardButton.BorderBrush = Brushes.Green;
                selectedGameboardButton.Content = itemSelector.Content;
            } else {
                selectedGameboardButton.Content = "";
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

            if (finalPlacedTiles.Count != locations.Count) {
                Console.WriteLine(string.Format("Locations count ({0}) does not match FinalPlacedTiles count ({1})! Cancelling save...", locations.Count, finalPlacedTiles.Count));
                return;
            }

            SaveTiles(finalPlacedTiles);
        }

        private void loadSave_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.OpenFileDialog openDialog = new System.Windows.Forms.OpenFileDialog();
            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            LoadTiles(openDialog.FileName);

            loadImageSrcLabel.Visibility = Visibility.Visible;
        }

        private void SaveTiles(List<Tile> finalPlacedTiles) {
            string[] savePathDirs = savePath.Split('\\');
            string relativeNotesPath = "notes_" + savePathDirs.Last();

            string notesSavePath = "";
            foreach (string dir in savePathDirs) {
                if (dir != savePathDirs.Last()) {
                    notesSavePath += dir + '\\';
                }
            }

            notesSavePath += relativeNotesPath;

            using (StreamWriter sw = new StreamWriter(savePath)) {
                /* File format:
                 * First line is # tiles wide, # tiles tall, # tiles (non-blank) per layer
                 * Second line is src file name
                 * After that, one line per tile
                 * Each line is leftHeight, rightHeight, then 1 or 0 for collision top, right, bottom, left, isEdge. 
                 *  Then mutexProperty, unit, item. 
                 *  Then for each layer, tile's xSrc and ySrc.
                 *  Then, if platform, for each platform destination, tile's xSrc and ySrc
                 * -x indicates x blank locations follow
                 * -1 inline indicates null tile on that layer
                 */
                string firstLine = levelWidthInTiles + " " + levelHeightInTiles + " ";
                for (int i = 0; i < numLayers; i++) {
                    firstLine += placedTiles[i].Count(t => t != null);
                    if (i != numLayers - 1)
                        firstLine += " ";
                }

                sw.WriteLine(firstLine);
                sw.WriteLine(loadImageSrcLabel.Content);

                for (int i = 0; i < locations.Count; i++) {
                    int nullCount = 0;
                    while (i < locations.Count && placedTiles.All(layer => layer[i] == null)) {
                        nullCount++;
                        i++;
                    }

                    if (nullCount != 0) {
                        sw.WriteLine("-" + nullCount);
                        nullCount = 0;

                        if (i >= locations.Count) break;
                    }

                    List<Tile> tilesAtLocation = new List<Tile>(numLayers);

                    foreach (var layer in placedTiles) {
                        if (layer[i] != null)
                            tilesAtLocation.Add(layer[i]);
                    }

                    int leftHeight = tilesAtLocation.Any(tile => tile.leftHeight != 0)
                        ? tilesAtLocation.First(tile => tile.leftHeight != 0).leftHeight
                        : 0;

                    int rightHeight = tilesAtLocation.Any(tile => tile.rightHeight != 0)
                        ? tilesAtLocation.First(tile => tile.rightHeight != 0).rightHeight
                        : 0;

                    int topCollision = tilesAtLocation.Any(tile => tile.topCollision) ? 1 : 0;
                    int rightCollision = tilesAtLocation.Any(tile => tile.rightCollision) ? 1 : 0;
                    int bottomCollision = tilesAtLocation.Any(tile => tile.bottomCollision) ? 1 : 0;
                    int leftCollision = tilesAtLocation.Any(tile => tile.leftCollision) ? 1 : 0;
                    int isEdge = tilesAtLocation.Any(tile => tile.isEdge) ? 1 : 0;

                    int mutexProperty = 0;
                    if (tilesAtLocation.Any(tile => tile.isPole))
                        mutexProperty = 1;
                    else if (tilesAtLocation.Any(tile => tile.isPoleEdge))
                        mutexProperty = 2;
                    else if (tilesAtLocation.Any(tile => tile.isDeadly))
                        mutexProperty = 3;

                    int unit = (int)locations[i].unit;
                    int item = (int)locations[i].item;

                    List<string> tileSourceCoordsList = new List<string>();

                    for (int j = 0; j < placedTiles.Count; j++) {
                        if (placedTiles[j][i] == null) {
                            tileSourceCoordsList.Add("-1");
                            continue;
                        }

                        tileSourceCoordsList.Add((placedTiles[j][i].x * tileWidth).ToString());
                        tileSourceCoordsList.Add((placedTiles[j][i].y * tileHeight).ToString());
                    }

                    string tileSourceCoords = string.Join(" ", tileSourceCoordsList);

                    string line =
                        leftHeight + " " +
                        rightHeight + " " +
                        topCollision + " " +
                        rightCollision + " " +
                        bottomCollision + " " +
                        leftCollision + " " +
                        isEdge + " " +
                        mutexProperty + " " +
                        unit + " " +
                        item;

                    if (tileSourceCoordsList.Any())
                        line += " " + tileSourceCoords;

                    if (locations[i].unit == UnitEnum.MovingPlatform) {
                        MovingPlatform platform = platforms.Single(p => p.buttonIndex == i);

                        foreach (Tuple<int, int> dest in platform.tileDests)
                            line += " " + dest.Item1 + " " + dest.Item2;
                    }

                    sw.WriteLine(line);
                }
            }

            using (StreamWriter sw = new StreamWriter(notesSavePath)) {
                foreach (List<Tile> layer in placedTiles) {
                    foreach (Tile tile in layer.Where(tile => tile != null)) {
                        sw.WriteLine(tile.notes);
                    }
                }
            }

            Console.WriteLine("File saved.");
        }

        private void LoadTiles(string fileName) {
            string[] loadPathDirs = fileName.Split('\\');
            string relativeNotesPath = "notes_" + loadPathDirs.Last();

            string notesLoadPath = "";
            foreach (string dir in loadPathDirs) {
                if (dir != loadPathDirs.Last()) {
                    notesLoadPath += dir + '\\';
                }
            }

            notesLoadPath += relativeNotesPath;

            using (StreamReader sr = new StreamReader(fileName)) {
                // Exception: Get first two lines differently
                // Line 1
                string line = sr.ReadLine();
                string[] line1Values = line.Split(' ');
                levelWidthInTiles = Convert.ToInt32(line1Values[0]);
                levelHeightInTiles = Convert.ToInt32(line1Values[1]);

                textboxLevelWidth.Text = levelWidthInTiles.ToString();
                textboxLevelHeight.Text = levelHeightInTiles.ToString();
                textboxNumLayers.Text = (line1Values.Length - 2).ToString();

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

                    List<Button> buttonsAtLocation = new List<Button>(numLayers);
                    for (int i = 0; i < numLayers; i++) {
                        buttonsAtLocation.Add(gameboardButtons[i][count]);
                    }

                    List<Tile> tilesAtLocation = new List<Tile>(numLayers);
                    for (int i = 0; i < numLayers; i++)
                        tilesAtLocation.Add(null);

                    int currentLayer = 0;
                    const int tileCoordFirstIndex = 10;
                    int tileCoordLastIndex = tileCoordFirstIndex - 1 + numLayers * 2;
                    for (int i = tileCoordFirstIndex; i <= tileCoordLastIndex; i++) {
                        if (i >= splitLine.Length)
                            break;

                        if (splitLine[i] == "-1") {
                            tileCoordLastIndex--;
                            currentLayer++;
                            continue;
                        }

                        if (i + 1 >= splitLine.Length) {
                            Console.WriteLine("Looking past splitLine's length for the y-coord of a tile. This shouldn't happen.");
                            break;
                        }

                        tilesAtLocation[currentLayer] = srcTiles.SingleOrDefault(t => t.x * tileWidth == int.Parse(splitLine[i]) && t.y * tileHeight == int.Parse(splitLine[i + 1]));
                        currentLayer++;
                        i++;
                    }

                    UnitEnum unit = UnitEnum.None;
                    ItemEnum item = ItemEnum.None;

                    unit = (UnitEnum)Enum.Parse(typeof(UnitEnum), splitLine[8]);
                    item = (ItemEnum)Enum.Parse(typeof(ItemEnum), splitLine[9]);

                    // If has unit or item, indicate this via string in the button
                    // For now, let's assume that the best button to indicate this in is in the foremost layer
                    if (unit != UnitEnum.None) {
                        foreach(Button button in buttonsAtLocation)
                            button.Content = unit.ToString();

                        int numNullsAtLocation = numLayers - tilesAtLocation.Count(tile => tile != null);
                        int numNotNullsAtLocation = numLayers - numNullsAtLocation;

                        // If moving platform, set up that object as well
                        if (unit == UnitEnum.MovingPlatform) {
                            int platformDestFirstIndex = tileCoordFirstIndex + numNotNullsAtLocation * 2 + numNullsAtLocation; 
                            MovingPlatform platform = GetOrCreatePlatform(count);

                            // Read in destinations from the last expected property to the end of the line
                            for (int i = platformDestFirstIndex; i < splitLine.Length; i += 2)
                                platform.tileDests.Add(new Tuple<int, int>(int.Parse(splitLine[i]), int.Parse(splitLine[i + 1])));

                            for (int i = 0; i < platform.tileDests.Count; i++) {
                                Button destButton = (Button)FindName("levelTile" + GetButtonIndexFromCoordinates(platform.tileDests[i]));
                                destButton.Content = "D" + (i + 1);
                            }

                            platforms.Add(platform);
                        }
                    }
                    if (item != ItemEnum.None) {
                        foreach(Button button in buttonsAtLocation)
                            button.Content = item.ToString();
                    }

                    // Set location properties
                    LocationData location = locations[count];
                    if (location != null) {
                        location.unit = unit;
                        location.item = item;
                    }

                    // Set button images for each tile at location
                    for (int i = 0; i < tilesAtLocation.Count; i++) {
                        if (tilesAtLocation[i] != null)
                            buttonsAtLocation[i].Background = new ImageBrush(tilesAtLocation[i].image.Source);
                    }

                    int? lastNonNullIndex = null;
                    for (int i = 0; i < tilesAtLocation.Count; i++) {
                        if (tilesAtLocation[i] == null)
                            continue;

                        placedTiles[i][count] = tilesAtLocation[i];
                        lastNonNullIndex = i;
                    }

                    // Set foremost-layer tile's properties
                    if (lastNonNullIndex.HasValue) {
                        Tile lastTile = placedTiles[lastNonNullIndex.Value][count];
                        lastTile.leftHeight = int.Parse(splitLine[0]);
                        lastTile.rightHeight = int.Parse(splitLine[1]);
                        lastTile.topCollision = splitLine[2].ToString() == "1" ? true : false;
                        lastTile.rightCollision = splitLine[3].ToString() == "1" ? true : false;
                        lastTile.bottomCollision = splitLine[4].ToString() == "1" ? true : false;
                        lastTile.leftCollision = splitLine[5].ToString() == "1" ? true : false;
                        lastTile.isEdge = splitLine[6].ToString() == "1" ? true : false;

                        // Mutex properties will default to false, so only need to think about setting them to true
                        string mutexProperty = splitLine[7].ToString();
                        if (mutexProperty == "1")
                            lastTile.isPole = true;
                        else if (mutexProperty == "2")
                            lastTile.isPoleEdge = true;
                        else if (mutexProperty == "3")
                            lastTile.isDeadly = true;
                    }

                    count++;
                }
            }

            Console.WriteLine("File loaded.");
            isLevelLoaded = true;
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
                    if (selectedTile.isPole) {
                        selectedTile.isPoleEdge = false;
                        selectedTile.isDeadly = false;
                        buttonIsPoleEdge.IsChecked = false;
                        buttonIsDeadly.IsChecked = false;
                    }

                    break;
                case "buttonIsPoleEdge":
                    selectedTile.isPoleEdge = selectedTile.isPoleEdge == true ? false : true;
                    buttonIsPoleEdge.IsChecked = selectedTile.isPoleEdge == true ? true : false;

                    // Reset other mutex properties
                    if (selectedTile.isPoleEdge) {
                        selectedTile.isPole = false;
                        selectedTile.isDeadly = false;
                        buttonIsPole.IsChecked = false;
                        buttonIsDeadly.IsChecked = false;
                    }

                    break;
                case "buttonIsDeadly":
                    selectedTile.isDeadly = selectedTile.isDeadly == true ? false : true;
                    buttonIsDeadly.IsChecked = selectedTile.isDeadly == true ? true : false;

                    // Reset other mutex properties
                    if (selectedTile.isDeadly) {
                        selectedTile.isPole = false;
                        selectedTile.isPoleEdge = false;
                        buttonIsPole.IsChecked = false;
                        buttonIsPoleEdge.IsChecked = false;
                    }

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

        private void buttonIsEdge_Click(object sender, RoutedEventArgs e) {
            if (selectedTile == null) return;

            selectedTile.isEdge = selectedTile.isEdge == true ? false : true;
            buttonIsEdge.IsChecked = selectedTile.isEdge == true ? true : false;
        }

        private void layerSelector_selectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!isLevelLoaded)
                return;

            // Clear board, then populate with appropriate buttons
            Body.Children.Clear();

            int tileIndex = 0;

            for (int i = 0; i < levelHeightInTiles; i++) {
                // Add "row"
                StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

                for (int j = 0; j < levelWidthInTiles; j++) {
                    Button button = gameboardButtons[layerSelector.SelectedIndex][tileIndex];
                    if (button.Parent != null)
                        ((StackPanel)button.Parent).Children.Clear();

                    stackPanel.Children.Add(button);
                    tileIndex++;
                }

                Body.Children.Add(stackPanel);
            }
        }

        private void toggleTileMode_Click(object sender, RoutedEventArgs e) {
            // This event fires AFTER the radio value switches, so the clicked value is the value we need to switch to
            // This also assumes the IsChecked field is never null because it's enabled by default

            if (radioPlaceTiles.IsChecked.Value) {
                // This is the default mode. This refers to selecting a tile from the left list and placing tiles on the right.
                selectedLocation = null;
            } else if (radioSelectTiles.IsChecked.Value) {
                // This is the optional mode. This refers to not being able to select a tile from the left list, but instead selecting tiles on the right.
                selectedTile = null;
            }
        }

        private int GetGameboardButtonIndex(Button button) {
            return gameboardButtons[layerSelector.SelectedIndex].IndexOf(button);
        }
    }
}
