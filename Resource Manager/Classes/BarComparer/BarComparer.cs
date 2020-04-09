using Archive_Unpacker.Classes.BarViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Resource_Manager.Classes.BarComparer
{
    public class BarComparer
    { 

        public IReadOnlyCollection<BarComparerEntry> CompareEntries { get; set; }

        private CollectionViewSource CompareEntriesCollection;

        public ICollectionView CompareSourceCollection
        {
            get
            {
                return this.CompareEntriesCollection.View;
            }
        }


        public static async Task<BarComparer> Compare(BarViewModel bar1, BarViewModel bar2)
        {

            BarComparer barComparer = new BarComparer();
            var barEntrys = new List<BarComparerEntry>();


            await Task.Run(() =>
            {
                var Added = bar2.barFile.BarFileEntrys.Where(item => !bar1.barFile.BarFileEntrys.Any(item2 => item2.FileNameWithRoot == item.FileNameWithRoot)).ToList();
                var Removed = bar1.barFile.BarFileEntrys.Where(item => !bar2.barFile.BarFileEntrys.Any(item2 => item2.FileNameWithRoot == item.FileNameWithRoot)).ToList();             
                var ChangedOld = bar1.barFile.BarFileEntrys.Where(item => bar2.barFile.BarFileEntrys.Any(item2 => item2.FileNameWithRoot == item.FileNameWithRoot && item2.CRC32 != item.CRC32)).ToList();
                var ChangedNew = bar2.barFile.BarFileEntrys.Where(item => bar1.barFile.BarFileEntrys.Any(item2 => item2.FileNameWithRoot == item.FileNameWithRoot && item2.CRC32 != item.CRC32)).ToList();
                var SameOld = bar1.barFile.BarFileEntrys.Where(item => bar2.barFile.BarFileEntrys.Any(item2 => item2.FileNameWithRoot == item.FileNameWithRoot && item2.CRC32 == item.CRC32)).ToList();
                var SameNew = bar2.barFile.BarFileEntrys.Where(item => bar1.barFile.BarFileEntrys.Any(item2 => item2.FileNameWithRoot == item.FileNameWithRoot && item2.CRC32 == item.CRC32)).ToList();


                for (int i = 0; i < ChangedOld.Count; i++)
                {
                    barEntrys.Add(new BarComparerEntry() { type = "Changed", entryNew = ChangedNew[i], entryOld = ChangedOld[i] });
                }

                Removed.ForEach(c => barEntrys.Add(new BarComparerEntry() { type = "Removed", entryOld = c, entryNew = null }));
                Added.ForEach(c => barEntrys.Add(new BarComparerEntry() { type = "Added", entryNew = c, entryOld = null }));
 
                for (int i = 0; i < SameOld.Count; i++)
                {
                    barEntrys.Add(new BarComparerEntry() { type = "Unchanged", entryNew = SameNew[i], entryOld = SameOld[i] });
                }
            }
            );


            barComparer.CompareEntries = new ReadOnlyCollection<BarComparerEntry>(barEntrys);
            barComparer.CompareEntriesCollection = new CollectionViewSource();
            barComparer.CompareEntriesCollection.Source = barComparer.CompareEntries;
            return barComparer;
        }

    }
}
