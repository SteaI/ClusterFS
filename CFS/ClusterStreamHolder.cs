using System.IO;

namespace CFS
{
    /// <summary>
    /// 클러스터에대한 바이너리 읽기,쓰기를 제공합니다.
    /// </summary>
    public class ClusterStreamHolder : FixedStream, ICluster
    {
        /// <summary>
        /// 클러스터의 인덱스를 가져옵니다.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// 클러스터 한 개의 크기를 가져옵니다.
        /// </summary>
        public int BlockSize { get; private set; }

        /// <summary>
        /// 확장된 클러스터의 갯수를 가져옵니다.
        /// </summary>
        public int Used { get; set; }

        /// <summary>
        /// 클러스터 제공자를 가져옵니다.
        /// </summary>
        public ClusterProvider Provider { get; private set; }
        
        long ICluster.Position
        {
            get
            {
                return base.Position;
            }
            set
            {
                base.Position = value;
            }
        }

        private CFSStream parentStream;

        // Original Stream 기반
        internal ClusterStreamHolder(ClusterProvider provider, CFSStream stream) : base(stream)
        {
            this.Provider = provider;
            parentStream = stream;
        }

        // Transaction or Memory 기반
        internal ClusterStreamHolder(ClusterProvider provider, Stream stream, CFSStream pStream) : base(stream)
        {
            this.Provider = provider;
            BlockSize = pStream.Header.ClusterSize;

            parentStream = pStream;
        }

        internal void SetRange(int index, long position, long size, int expand)
        {
            base.SetRange(position, size);

            this.BlockSize = parentStream.Header.ClusterSize;
            this.Used = expand;
            this.Index = index;
        }

        internal void UpdateExpand()
        {
            long size = this.Size + Constant.CLUSTER_SIZE;

            SetExpand((int)(size / BlockSize));
        }

        internal void SetExpand(int used)
        {
            long pos = Stream.Position;

            this.Used = used;

            Stream.Seek(Position - Constant.CLUSTER_SIZE, SeekOrigin.Begin);
            Writer.Write(Used);

            Stream.Seek(pos, SeekOrigin.Begin);
        }

        internal ClusterSurface ToSurface(long offset)
        {
            return new ClusterSurface()
            {
                Used = this.Used,
                Position = Position + offset
            };
        }

        /// <summary>
        /// 데이터를 쓰기 전 고정된 스트림에 대한 남은 바이트 수를 계산하여 클러스터를 확장하고 예외를 검출합니다.
        /// </summary>
        /// <param name="size">데이터의 바이트수 입니다.</param>
        protected override void CheckWriteError(int size)
        {
            while (size > Remain)
            {
                if (!Provider.TryExpand(this))
                    throw new EndOfStreamException();
            }
        }
    }
}
