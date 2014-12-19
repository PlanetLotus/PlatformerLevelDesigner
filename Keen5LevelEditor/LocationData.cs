using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen5LevelEditor {
    public class LocationData {
        public UnitEnum unit { get; set; }
        public ItemEnum item { get; set; }

        public LocationData() {
            unit = UnitEnum.None;
            item = ItemEnum.None;
        }
    }
}
