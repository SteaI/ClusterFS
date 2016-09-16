using System.Runtime.InteropServices;

namespace ClusterFS
{
    /// <summary>
    /// Cluster File System의 헤더 구조체 입니다.
    /// </summary>
    public struct CFSHeader
    {
        /// <summary>
        /// 스트림을 생성할 때 지정된 버전 입니다.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 스트림을 생성한 시간입니다.
        /// </summary>
        public long Date { get; set; }

        /// <summary>
        /// 스트림을 생성할 때 지정된 한 클러스터의 크기입니다.
        /// </summary>
        public int ClusterSize { get; set; }

        /// <summary>
        /// 최대 확장 가능한 클러스터의 갯수입니다.
        /// </summary>
        public int ClusterMaxExpand { get; set; }
    }
}
