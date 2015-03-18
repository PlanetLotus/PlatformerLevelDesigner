using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen5LevelEditor {
    public sealed class MovingPlatform {
        public int buttonIndex { get; set; }
        public int startX { get; set; }
        public int startY { get; set; }
        public List<Tuple<int, int>> tileDests { get; set; }
    }
}
