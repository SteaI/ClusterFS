using System;

namespace CFS.Exceptions
{
    class ClusterNotFoundException : Exception
    {
        public ClusterNotFoundException(string message) : base(message)
        {
        }
    }
}
