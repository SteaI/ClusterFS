using System;

namespace CFS.Exceptions
{
    class CFSException : Exception
    {
        public CFSException(string message) : base(message)
        {
        }
    }
}
