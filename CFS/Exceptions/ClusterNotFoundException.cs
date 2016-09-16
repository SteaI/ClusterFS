using System;

namespace ClusterFS.Exceptions
{
    class ClusterNotFoundException : Exception
    {
        public ClusterNotFoundException(string message) : base(message)
        {
        }
    }
}
