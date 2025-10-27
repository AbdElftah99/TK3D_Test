using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TK3D_Test.Core
{
    public enum CharClass
    {
        Digit,
        Letter,
        Other
    }
    public class OrdinalStringComparer : IComparer<string>
    {
        private readonly bool _ignoreCase;

        private readonly Func<string, string, int> _compareFunction;

        public OrdinalStringComparer()
            : this(ignoreCase: true)
        {
        }

        public OrdinalStringComparer(bool ignoreCase)
        {
            _ignoreCase = ignoreCase;
        }

        public OrdinalStringComparer(Func<string, string, int> compareFunction)
        {
            _compareFunction = compareFunction;
        }

        public int Compare(string x, string y)
        {
            if (_compareFunction != null)
            {
                return _compareFunction(x, y);
            }

            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            StringComparison comparisonType = (_ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
            string[] array = SplitIntoParts(x);
            string[] array2 = SplitIntoParts(y);
            for (int i = 0; i < Math.Max(array.Length, array2.Length); i++)
            {
                if (i >= array.Length)
                {
                    return -1;
                }

                if (i >= array2.Length)
                {
                    return 1;
                }

                string text = array[i];
                string text2 = array2[i];
                long result;
                bool flag = long.TryParse(text, out result);
                long result2;
                bool flag2 = long.TryParse(text2, out result2);
                if (flag && flag2)
                {
                    int num = result.CompareTo(result2);
                    if (num != 0)
                    {
                        return num;
                    }

                    num = text.Length.CompareTo(text2.Length);
                    if (num != 0)
                    {
                        return num;
                    }

                    continue;
                }

                if (flag != flag2)
                {
                    return (!flag) ? 1 : (-1);
                }

                bool flag3 = IsLettersOnly(text);
                bool flag4 = IsLettersOnly(text2);
                if (flag3 && flag4)
                {
                    int num2 = string.Compare(text, text2, comparisonType);
                    if (num2 != 0)
                    {
                        return num2;
                    }

                    continue;
                }

                if (!flag3 && !flag4)
                {
                    int num3 = string.Compare(text, text2, StringComparison.Ordinal);
                    if (num3 != 0)
                    {
                        return num3;
                    }

                    continue;
                }

                return (!flag3) ? 1 : (-1);
            }

            return 0;
        }

        private static bool IsLettersOnly(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsLetter(c))
                {
                    return false;
                }
            }

            return s.Length > 0;
        }

        private static CharClass Classify(char ch)
        {
            if (char.IsDigit(ch))
            {
                return CharClass.Digit;
            }

            if (char.IsLetter(ch))
            {
                return CharClass.Letter;
            }

            return CharClass.Other;
        }

        private static string[] SplitIntoParts(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return [];
            }

            List<string> list = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();
            CharClass charClass = Classify(input[0]);
            stringBuilder.Append(input[0]);
            for (int i = 1; i < input.Length; i++)
            {
                char c = input[i];
                CharClass charClass2 = Classify(c);
                if (charClass2 == charClass)
                {
                    stringBuilder.Append(c);
                    continue;
                }

                list.Add(stringBuilder.ToString());
                stringBuilder.Clear();
                stringBuilder.Append(c);
                charClass = charClass2;
            }

            list.Add(stringBuilder.ToString());
            return [.. list];
        }
    }
}
