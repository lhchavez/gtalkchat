using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;

namespace Gchat.Utilities {
    public static class Extensions {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection) {
            var o = new ObservableCollection<T>();

            foreach (T item in collection) {
                o.Add(item);
            }

            return o;
        }

        public static void Sort<T>(this ObservableCollection<T> collection) {
            collection.Sort(null);
        }

        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison) {
            List<T> sorted = collection.ToList();

            if (comparison != null) {
                sorted.Sort(comparison);
            } else {
                sorted.Sort();
            }

            collection.Clear();

            foreach (T item in sorted) {
                collection.Add(item);
            }
        }
    }
}