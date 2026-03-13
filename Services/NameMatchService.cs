using System;
using Claytree.Risk.Functions.Interface;

namespace Claytree.Risk.Functions.Services
{
    public class NameMatchService : INameMatchService
    {
        public double ComputeScore(string inputName, string panName)
        {
            if (string.IsNullOrWhiteSpace(inputName) || string.IsNullOrWhiteSpace(panName))
                return 0;

            inputName = inputName.ToLower().Trim();
            panName = panName.ToLower().Trim();

            if (inputName == panName)
                return 1.0;

            var distance = Levenshtein(inputName, panName);
            var max = Math.Max(inputName.Length, panName.Length);

            return 1.0 - ((double)distance / max);
        }

        private int Levenshtein(string s, string t)
        {
            var d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= t.Length; j++) d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s.Length, t.Length];
        }
    }
}
