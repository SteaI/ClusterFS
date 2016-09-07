namespace CFS
{
    struct ClusterSurface : ICluster
    {
        public int Used { get; set; }
        public long Position { get; set; }

        public override string ToString()
        {
            return $"{{Position: {Position}, Used: {Used}}}";
        }
    }
}