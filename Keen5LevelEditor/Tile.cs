using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Keen5LevelEditor {
    class Tile {
        public Image image;
        public int x;
        public int y;

        public Tile(CroppedBitmap src, int x, int y) {
            image = new Image();
            image.Source = src;
            this.x = x;
            this.y = y;
        }
    }
}
