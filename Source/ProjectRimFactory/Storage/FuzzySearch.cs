using System;

namespace ProjectRimFactory.Storage.UI
{
    // Helper class made from 
    // https://github.com/kdjones/fuzzystring/blob/1828d564bf9e3b2cda0eb81970d53561573b5def/FuzzyString/LevenshteinDistance.cs#L21
    public static class FuzzySearch
    {
        public struct Strength
        {
            public const double Strong = 0.25;
            public const double Normal = 0.50;
            public const double Manual = 0.6;
            public const double Weak = 0.75;

        }

        private static int HammingDistance(this string source, string target)
        {
            int distance = 0;

            if (source.Length == target.Length)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (!source[i].Equals(target[i]))
                    {
                        distance++;
                    }
                }
                return distance;
            }
            else { return 99999; }
        }
        
        /// <summary>
        /// Calculate the minimum number of single-character edits needed to change the source into the target,
        /// allowing insertions, deletions, and substitutions.
        /// <br/><br/>
        /// Time complexity: at least O(n^2), where n is the length of each string
        /// Accordingly, this algorithm is most efficient when at least one of the strings is very short
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns>The number of edits required to transform the source into the target. This is at most the length of the longest string, and at least the difference in length between the two strings</returns>
        private static int LevenshteinDistance(this string source, string target)
        {
            var sourceLength = source.Length;
            var targetLength = target.Length;

            var matrix = new int[sourceLength + 1, targetLength + 1];

            // First calculation, if one entry is empty return full length
            if (sourceLength == 0)
                return targetLength;

            if (targetLength == 0)
                return sourceLength;

            // Initialization of matrix with row size sourceLength and columns size targetLength
            for (var i = 0; i <= sourceLength; matrix[i, 0] = i++) { }
            for (var j = 0; j <= targetLength; matrix[0, j] = j++) { }

            // Calculate rows and collumns distances
            for (var i = 1; i <= sourceLength; i++)
            {
                for (var j = 1; j <= targetLength; j++)
                {
                    var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            // return result
            return matrix[sourceLength, targetLength];
        }

        /// <summary>
        /// Calculate the minimum number of single-character edits needed to change the source into the target,
        /// allowing insertions, deletions, and substitutions.
        /// <br/><br/>
        /// Time complexity: at least O(n^2), where n is the length of each string
        /// Accordingly, this algorithm is most efficient when at least one of the strings is very short
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns>The Levenshtein distance, normalized so that the lower bound is always zero, rather than the difference in length between the two strings</returns>
        private static double NormalizedLevenshteinDistance(this string source, string target)
        {
            var unnormalizedLevenshteinDistance = source.LevenshteinDistance(target);

            return unnormalizedLevenshteinDistance - source.LevenshteinDistanceLowerBounds(target);
        }

        /// <summary>
        /// The upper bounds is either the length of the longer string, or the Hamming distance.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int LevenshteinDistanceUpperBounds(this string source, string target)
        {
            // If the two strings are the same length then the Hamming Distance is the upper bounds of the Levenshtien Distance.
            if (source.Length == target.Length) { return source.HammingDistance(target); }

            // Otherwise, the upper bound is the length of the longer string.

            if (source.Length > target.Length) { return source.Length; }

            return target.Length > source.Length ? target.Length : 9999;
        }

        /// <summary>
        /// The lower bounds is the difference in length between the two strings
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int LevenshteinDistanceLowerBounds(this string source, string target)
        {
            // If the two strings are different lengths then the lower bounds is the difference in length.
            return Math.Abs(source.Length - target.Length);
        }

        public static double NormalizedFuzzyStrength(this string source, string target)
        {
            return Convert.ToDouble(source.NormalizedLevenshteinDistance(target)) / Convert.ToDouble(
                Math.Max(source.Length, target.Length) - source.LevenshteinDistanceLowerBounds(target));
        }
        
        public static double FuzzyStrength(this string source, string target)
        {
            return Convert.ToDouble(source.LevenshteinDistance(target)) /
                           Convert.ToDouble(source.LevenshteinDistanceUpperBounds(target));
            // Log.Warning($"Current strength: {strength.ToString(CultureInfo.InvariantCulture)}", true);;
        }
        
    }
}