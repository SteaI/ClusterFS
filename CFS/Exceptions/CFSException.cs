using System;

namespace ClusterFS.Exceptions
{
    class CFSException : Exception
    {
        public CFSException(string message) : base(message)
        {
        }
    }
}
