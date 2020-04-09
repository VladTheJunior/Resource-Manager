using Resource_Manager.Classes.Bar;

namespace Resource_Manager.Classes.BarComparer
{
    public class BarComparerEntry
    {
        public string type { get; set; } = "Unchanged";
        public BarEntry entryOld { get; set; }
        public BarEntry entryNew { get; set; }    
    }
}
