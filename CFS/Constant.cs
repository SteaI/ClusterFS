namespace ClusterFS
{
    internal static class Constant
    {
        public const int HEADER_SIZE = 32;
        public const int HEADER_POSITION = 0;

        public const int VERSION_SIZE = 16;
        public const int VERSION_POSITION = HEADER_POSITION;

        public const int DATE_SIZE = 8;
        public const int DATE_POSITION = VERSION_POSITION + VERSION_SIZE;

        public const int CLUSTER_SIZE = 4;
        public const int CLUSTER_POSITION = DATE_POSITION + DATE_SIZE;

        public const int CLUSTER_EXPAND_SIZE = 4;
        public const int CLUSTER_EXPAND_POSITION = CLUSTER_POSITION + CLUSTER_SIZE;

        public const int CLUSTER_COUNT_SIZE = 8;
        public const int CLUSTER_COUNT_POSITION = HEADER_SIZE;

        public const int CLUSTER_AREA_POSITION = CLUSTER_COUNT_POSITION + CLUSTER_COUNT_SIZE;
    }
}
