namespace ClusterFS
{
    /// <summary>
    /// 클러스터 인터페이스입니다.
    /// </summary>
    public interface ICluster
    {
        /// <summary>
        /// 확장된 클러스터의 갯수를 가져옵니다.
        /// </summary>
        int Used { get; set; }

        /// <summary>
        /// 스트림내 클러스터의 위치를 가져옵니다.
        /// </summary>
        long Position { get; set; }
    }
}
