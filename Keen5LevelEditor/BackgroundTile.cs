using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Keen5LevelEditor {
    public class BackgroundTile {
        public BackgroundTile(CroppedBitmap src, int x, int y) {
            image = new Image();
            image.Source = src;
            this.x = x;
            this.y = y;
        }

        public Image image;
        public int x;
        public int y;
    }
}
