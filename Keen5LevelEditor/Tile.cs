using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Keen5LevelEditor {
    public class Tile {
        public Image image;
        public int x;
        public int y;
        public int leftHeight;
        public int rightHeight;

        public bool topCollision { get; set; }
        public bool rightCollision { get; set; }
        public bool bottomCollision { get; set; }
        public bool leftCollision { get; set; }
        public bool isPole { get; set; }
        public bool isPoleEdge { get; set; }
        public bool isDeadly { get; set; }
        public bool isEdge { get; set; }
        public string notes { get; set; }

        public Tile(CroppedBitmap src, int x, int y) {
            image = new Image();
            image.Source = src;
            this.x = x;
            this.y = y;

            leftHeight = 0;
            rightHeight = 0;
            topCollision = false;
            rightCollision = false;
            bottomCollision = false;
            leftCollision = false;
            isPole = false;
            isPoleEdge = false;
            isEdge = false;
            notes = "";
        }
    }
}
