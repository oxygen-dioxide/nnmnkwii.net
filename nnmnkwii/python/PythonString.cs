using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace nnmnkwii.python
{
    //python string methods that C# doesn't have
    public static class PythonString
    {
        //python "".split() without parameter
        public static string[] split(string str)
        {
            return Regex.Split(str, @" +");
        }
    }
}
