using System.Collections.Generic;

namespace SubTask.ObjectSelection
{
    internal class Str
    {
        


        


        public static string Join(params string[] parts)
        {
            return string.Join("_", parts);
        }

        public static string GetIndexedStr(string str, int num)
        {
            return str.Insert(3, num.ToString());
        }

        public static string GetCountedStr(string str, int count)
        {
            return str + count.ToString();
        }

    }
}
