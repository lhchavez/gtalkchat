using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace gtalkchat
{
    public static class Extensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> obj)
        {
            var o = new ObservableCollection<T>();

            foreach (T item in obj)
            {
                o.Add(item);
            }

            return o;
        }
    }
}
