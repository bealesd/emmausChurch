using System;
using System.Collections.Generic;
using System.Linq;

namespace Emmaus.Helper
{
    public static class RotaHelper
    {
        public static List<string> JoinedCapitalizedNamesToSpaceSeparatedNames(this List<string> names)
        {
            List<string> newNames = new List<string>();
            foreach (string name in names)
            {
                List<char> charArray = new List<char>();
                foreach (char letter in name)
                {
                    if (char.IsUpper(letter))
                    {
                        charArray.Add(' ');
                    }
                    charArray.Add(letter);
                }
                newNames.Add(string.Concat(charArray).Trim());
            }
            return newNames;
        }

        public static string JoinedCapitalizedNameToSpaceSeparatedName(this string name)
        {
            List<char> charArray = new List<char>();
            foreach (char letter in name)
            {
                if (char.IsUpper(letter))
                {
                    charArray.Add(' ');
                }
                charArray.Add(letter);
            }
            return string.Concat(charArray).Trim();
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> known = new HashSet<TKey>();
            return source.Where(element => known.Add(keySelector(element)));
        }
    }
}
