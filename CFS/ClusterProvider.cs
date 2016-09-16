namespace ClusterFS
{
    /// <summary>
    /// 클러스터를 생성 및 삭제를 제공합니다.
    /// </summary>
    public abstract class ClusterProvider
    {
        /// <summary>
        /// 지정된 크기의 새로운 클러스터 영역을 생성합니다.
        /// </summary>
        /// <returns>클러스터입니다.</returns>
        public abstract ClusterStreamHolder CreateCluster();

        /// <summary>
        /// 클러스터를 제거합니다.
        /// </summary>
        /// <param name="idx">클러스터 인덱스입니다.</param>
        /// <param name="architect">인덱스 아키텍쳐를 지정합니다.</param>
        public abstract void RemoveCluster(int idx, Architecture architect);

        internal abstract bool TryExpand(ClusterStreamHolder holder);
    }
}
