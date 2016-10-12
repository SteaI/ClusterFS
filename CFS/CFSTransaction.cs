using ClusterFS.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClusterFS
{
    /// <summary>
    /// 개념적인 계층을 위한 트랜잭션을 나타내는 클래스입니다.
    /// </summary>
    public class CFSTransaction : ClusterProvider, IDisposable
    {
		public static class Config
		{
			public static string BaseDirectory { get; set; }
		}

        internal ClusterStreamHolder StreamHolder { get; }
        internal CFSStream BaseStream { get; }
        internal FileStream Stream { get; }
        internal List<ClusterSurface> Clusters { get; } = new List<ClusterSurface>();

        internal CFSTransaction(CFSStream stream, FileStream fStream)
        {
            this.BaseStream = stream;
            this.Stream = fStream;
            this.StreamHolder = new ClusterStreamHolder(this, fStream, stream);

            Clear();
        }

        internal override bool TryExpand(ClusterStreamHolder holder)
        {
            long totLength = holder.Position + holder.Size + holder.BlockSize;

            if (Stream.Length < totLength)
            {
                CreateBufferBlock(16);
            }

            holder.SetRange(holder.Position, (holder.Used + 1) * holder.BlockSize - Constant.CLUSTER_SIZE, false);
            holder.UpdateExpand();

            var surface = Clusters[Clusters.Count - 1];
            surface.Used = holder.Used;

            Clusters[Clusters.Count - 1] = surface;

            return true;
        }

        private void CreateBufferBlock(int count)
        {
            Stream.SetLength(Stream.Length + BaseStream.Header.ClusterSize * count);
        }

        /// <summary>
        /// 지정된 크기의 새로운 클러스터 영역을 생성합니다.
        /// </summary>
        /// <returns>읽기,쓰기를 제공하는 클러스터입니다.</returns>
        public override ClusterStreamHolder CreateCluster()
        {
            long position = 0;

            if (Clusters.Count > 0)
            {
                var surface = Clusters[Clusters.Count - 1];

                position = (surface.Position - Constant.CLUSTER_SIZE) + surface.Used * BaseStream.Header.ClusterSize;
            }

            if (Stream.Length < position + BaseStream.Header.ClusterSize)
            {
                CreateBufferBlock(128);
            }
            
            position += Constant.CLUSTER_SIZE;

            StreamHolder.SetRange(Clusters.Count, position, BaseStream.Header.ClusterSize - Constant.CLUSTER_SIZE, 1);
            StreamHolder.UpdateExpand();

            Clusters.Add(new ClusterSurface()
            {
                Used = StreamHolder.Used,
                Position = position
            });

            return StreamHolder;
        }

        /// <summary>
        /// 클러스터를 제거합니다.
        /// </summary>
        /// <param name="idx">클러스터 인덱스입니다.</param>
        /// <param name="architect">인덱스 아키텍쳐를 지정합니다.</param>
        public override void RemoveCluster(int idx, Architecture architect)
        {
            throw new CFSException("트랜잭션에서 클러스터를 삭제할 수 없습니다.");
        }

        /// <summary>
        /// 트랜잭션을 CFS 스트림에 커밋합니다.
        /// </summary>
        public void Commit()
        {
            BaseStream.Commit();
        }

        /// <summary>
        /// 트랜잭션을 커밋하고 종료합니다.
        /// </summary>
        public void Close()
        {
            BaseStream.EndTransaction();
        }

        /// <summary>
        /// <see cref="CFSTransaction"/>에서 사용하는 모든 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (BaseStream.transaction != null)
                Close();
        }

        internal static CFSTransaction OpenTransaction(CFSStream stream)
        {
            string fileName;

            for (int i = 0; true; i++)
            {
				fileName = $"{Config.BaseDirectory}~cfs_{i}.tmp";

                if (!File.Exists(fileName))
                    break;
            }

            return new CFSTransaction(stream,
                File.Create(fileName, stream.Header.ClusterSize * 128, FileOptions.DeleteOnClose));
        }

        internal void Clear()
        {
            Stream.SetLength(BaseStream.Header.ClusterSize * 128);
            Stream.Seek(0, SeekOrigin.Begin);

            Clusters.Clear();
        }
    }
}