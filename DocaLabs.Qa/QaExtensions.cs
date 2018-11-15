using System.Collections.Generic;

namespace DocaLabs.Qa
{
    public static class QaExtensions
    {
        public static string[] ToConfigurationArgs(this ICollection<KeyValuePair<string, string>> dic)
        {
            var result = new string[dic.Count];

            var i = 0;

            foreach (var pair in dic)
                result[i++] = $"{pair.Key}={pair.Value}";

            return result;
        }
        public static string[] ToConfigurationArgs(this string input)
        {
            return JsonQaConfigurationParser.Parse(input).ToConfigurationArgs();
        }

        public static string[] ToConfigurationArgs(this object input)
        {
            return JsonQaConfigurationParser.Parse(input).ToConfigurationArgs();
        }

        public static T[] MergeRange<T>(this T[] source, ICollection<T> values)
        {
            var result = new List<T>(source);

            result.AddRange(values);

            return result.ToArray();
        }
    }
}
