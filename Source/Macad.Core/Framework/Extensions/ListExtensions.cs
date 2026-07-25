using Macad.Occt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Macad.Core;

public static class ListExtensions
{
    public static bool ContainsSame<T>(this IEnumerable<T> list, TopoDS_Shape shape) where T : TopoDS_Shape
    {
        if (shape == null)
        {
            foreach (var item in list)
            {
                if (item == null)
                    return true;
            }
            return false;
        }
        else
        {
            foreach (var item in list)
            {
                if (shape.IsSame(item))
                    return true;
            }
            return false;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public static bool ContainsPartner<T>(this IEnumerable<T> list, TopoDS_Shape shape) where T : TopoDS_Shape
    {
        if (shape == null)
        {
            foreach (var item in list)
            {
                if (item == null)
                    return true;
            }
            return false;
        }
        else
        {
            foreach (var item in list)
            {
                if (shape.IsPartner(item))
                    return true;
            }
            return false;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public static bool ContainsAllSame<T>(this IEnumerable<T> source, IEnumerable<T> other) where T : TopoDS_Shape
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (other == null) throw new ArgumentNullException(nameof(other));

        var sources = source.ToList();

        using (var otherIterator = other.GetEnumerator())
        {
            while (otherIterator.MoveNext())
            {
                var candidate = otherIterator.Current;
                if (!sources.ContainsSame(candidate))
                {
                    return false;
                }
            }
            return true;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public static int IndexOfSame<T>(this IList<T> list, TopoDS_Shape item) where T : TopoDS_Shape
    {
        int size = list.Count;
        if (item == null)
        {
            for (int i = 0; i < size; i++)
                if (list[i] == null)
                    return i;
            return -1;
        }
        else
        {
            for (int i = 0; i < size; i++)
            {
                if (item.IsSame(list[i]))
                    return i;
            }
            return -1;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public static T FirstSameOrDefault<T>(this IEnumerable<T> enumerable, TopoDS_Shape shape) where T : TopoDS_Shape
    {
        return enumerable.FirstOrDefault(x => x.IsSame(shape));
    }

    //--------------------------------------------------------------------------------------------------

    public static TValue FirstSameOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TopoDS_Shape shape) where TKey : TopoDS_Shape
    {
        return dict.FirstOrDefault(kvp => kvp.Key.IsSame(shape)).Value;
    }

    //--------------------------------------------------------------------------------------------------

}