using System;
using System.Collections.Generic;
using System.Text;

namespace nnmnkwii.python
{
    public class AssertionError : Exception
    {

        public AssertionError() : base() { }

        public AssertionError(string message) : base(message)
        {
        }

        public AssertionError(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public static class PythonAssert
    {
        public static void Assert(bool expression)
        {
            if (!expression)
            {
                throw new AssertionError();
            }
        }

        public static void Assert(bool expression, string message)
        {
            if (!expression)
            {
                throw new AssertionError(message);
            }
        }
    }
}
