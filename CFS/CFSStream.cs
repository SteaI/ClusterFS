using ClusterFS.Exceptions;
using ClusterFS.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClusterFS
{
    /// <summary>
    /// 스트림 대해 Cluster File System 포맷을 제공하여 읽기/쓰기 작업을 모두 지원합니다.
    /// </summary>
    public class CFSStream : ClusterProvider, IDisposable
    {
        // * Cluster 열거의 속도를 위해 한 객체로 돌림빵
        private ClusterStreamHolder streamHolder;
        private List<ClusterSurface> clusterStatus;

        internal CFSTransaction transaction;
        internal BinaryWriter writer;
        internal BinaryReader reader;

        #region 프로퍼티
        /// <summary>
        /// <see cref="CFSStream"/>의 원본 스트림에 대한 액세스를 노출합니다.
        /// </summary>
        public Stream BaseStream { get; private set; }

        /// <summary>
        /// CFS 포맷의 헤더를 가져옵니다.
        /// </summary>
        public CFSHeader Header { get; private set; }

        /// <summary>
        /// 스트림이 CFS 포맷인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsValidStream { get; private set; }

        /// <summary>
        /// 물리적인 클러스터 갯수를 가져옵니다.
        /// </summary>
        public long ClusterCount { get; private set; }

        /// <summary>
        /// 논리적인 클러스터 갯수를 가져옵니다.
        /// </summary>
        public long ClusterLength { get { return clusterStatus.Count; } }
        #endregion

        #region 생성자
        /// <summary>
        /// <see cref="CFSStream"/> 포맷 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="stream">I/O 스트림입니다.</param>
        public CFSStream(Stream stream)
        {
            InitStream(stream);
            InitCFSStream();

            // caching
            clusterStatus = AllClusters(Architecture.Physical, true)
                .Select(c => c.ToSurface(-Constant.CLUSTER_SIZE))
                .ToList();
        }

        /// <summary>
        /// <see cref="CFSStream"/> 포맷 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="stream">I/O 스트림입니다.</param>
        /// <param name="version">CFS파일 버전입니다.</param>
        /// <param name="clusterSize">클러스터 한 블럭의 크기입니다.</param>
        /// <param name="clusterMaxExpand">확장 가능한 최대 클러스터 갯수 입니다.</param>
        /// <param name="capacity">클러스터의 제안된 시작 갯수 입니다.</param>
        public CFSStream(Stream stream, string version, int clusterSize, int clusterMaxExpand, int capacity)
        {
            if (Constant.CLUSTER_SIZE >= clusterSize)
                throw new CFSException("클러스터 크기가 너무 작습니다.");

            stream.SetLength(0);

            InitStream(stream);

            CreateHeader(version, clusterSize, clusterMaxExpand);
            CreateClusterArea(clusterSize, capacity);

            InitCFSStream();

            clusterStatus = new List<ClusterSurface>();
            for (int i = 0; i < capacity; i++)
            {
                clusterStatus.Add(new ClusterSurface()
                {
                    Position = Constant.CLUSTER_AREA_POSITION + i * clusterSize
                });
            }
        }

        private void InitCFSStream()
        {
            CheckStructure();

            IsValidStream = true;
            Header = ReadHeader();

            this.ClusterCount = GetClusterCount();
        }

        private void InitStream(Stream stream)
        {
            if (!stream.CanRead || !stream.CanWrite || !stream.CanSeek)
                throw new NotSupportedException();

            BaseStream = stream;

            writer = new BinaryWriter(stream, Encoding.UTF8);
            reader = new BinaryReader(stream, Encoding.UTF8);

            streamHolder = new ClusterStreamHolder(this, this);
        }
        #endregion

        #region 사용자 함수
        /// <summary>
        /// 현재 스트림을 닫고 현재 스트림과 관련된 소켓과 파일 핸들 등의 리소스를 모두 해제합니다. 이 메서드를 호출하는 대신 스트림이 올바르게 삭제되었는지
        /// 확인합니다.
        /// </summary>
        public void Close()
        {
            BaseStream.Close();
        }

        /// <summary>
        /// <see cref="CFSStream"/>에서 사용하는 모든 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            BaseStream?.Dispose();
            writer?.Dispose();
            reader?.Dispose();
            transaction?.Stream.Dispose();

            streamHolder = null;
            transaction = null;
            clusterStatus = null;
        }

        /// <summary>
        /// 모든 클러스터 스트림을 가져옵니다.
        /// </summary>
        /// <returns>읽기,쓰기를 제공하는 열거형 클러스터입니다.</returns>
        public IEnumerable<ClusterStreamHolder> AllClusters(Architecture architect = Architecture.Logical, bool all = false)
        {
            long count = (architect == Architecture.Physical ? ClusterCount : ClusterLength);

            for (int i = 0; i < count; i++)
            {
                PeekCluster(i, architect);
                int status = streamHolder.Used;

                if (status == 0)
                {
                    status = 1;

                    if (all)
                        yield return streamHolder;
                }
                else
                    yield return streamHolder;

                if (architect == Architecture.Physical)
                    i += (status - 1);
            }
        }

        /// <summary>
        /// 지정된 크기의 새로운 클러스터 영역을 생성합니다.
        /// </summary>
        /// <returns>읽기,쓰기를 제공하는 클러스터입니다.</returns>
        public override ClusterStreamHolder CreateCluster()
        {
            const int bufferBlockSize = 1;

            if (transaction != null)
                return transaction.CreateCluster();
            
            // 사용 가능한 클러스터 - Stream
            int free = FindFreeClusterPosition(1); // useable position
            long cpPos = Constant.CLUSTER_AREA_POSITION + Header.ClusterSize * ClusterCount;

            long totLength = GetStructureLength() + Header.ClusterSize * bufferBlockSize;

            if (free != -1)
            {
                cpPos = clusterStatus[free].Position;
                totLength = Math.Max(cpPos + Header.ClusterSize, this.BaseStream.Length);

                var data = clusterStatus[free];
                data.Used = 1;

                clusterStatus[free] = data;
            }
            else
            {
                clusterStatus.Add(new ClusterSurface()
                {
                    Position = cpPos,
                    Used = 1
                });
            }

            if (this.BaseStream.Length < totLength)
            {
                this.BaseStream.SetLength(totLength);
                UpdateClusterCount();
            }

            streamHolder.SetRange(clusterStatus.Count, cpPos + Constant.CLUSTER_SIZE, Header.ClusterSize - Constant.CLUSTER_SIZE, 1);
            streamHolder.UpdateExpand();

            return streamHolder;
        }

        /// <summary>
        /// 클러스터를 제거합니다.
        /// </summary>
        /// <param name="idx">클러스터 인덱스입니다.</param>
        /// <param name="architect">인덱스 아키텍쳐를 지정합니다.</param>
        public override void RemoveCluster(int idx, Architecture architect = Architecture.Logical)
        {
            ClusterStreamHolder holder = PeekCluster(idx, architect);

            int lidx = idx;
            if (architect == Architecture.Physical)
                lidx = clusterStatus.FindIndex(c => c.Position == holder.Position - Constant.CLUSTER_SIZE);

            ClusterSurface surface = clusterStatus[lidx];
            surface.Used = 0;

            clusterStatus[lidx] = surface;

            if (holder.Used > 1)
            {
                clusterStatus.InsertRange(lidx + 1,
                    Enumerable.Range(1, holder.Used - 1).Select(
                        i => new ClusterSurface()
                        {
                            Position = surface.Position + i * Header.ClusterSize
                        }));
            }

            for (int i = 0; i < holder.Used; i++)
            {
                var s = clusterStatus[i + lidx];

                Seek(s.Position);
                writer.Write(0);
            }
        }

        /// <summary>
        /// 지정된 인덱스에 있는 클러스터 스트림을 가져옵니다.
        /// </summary>
        /// <param name="idx">클러스터 번호입니다.</param>
        /// <param name="architect">클러스터를 가져올 아키텍쳐입니다.</param>
        /// <returns>읽기,쓰기를 제공하는 클러스터입니다.</returns>
        public ClusterStreamHolder PeekCluster(int idx, Architecture architect = Architecture.Logical)
        {
            MoveToCluster(idx, architect);

            return streamHolder;
        }

        /// <summary>
        /// CFS 파일 데이터 무결성 검사를 진행합니다.
        /// </summary>
        /// <returns>데이터 무결성 여부를 반환합니다.</returns>
        public bool IntegrityCheck()
        {
            CheckStructure();

            int rClusterCount = 0;

            for (int i = 0; i < this.ClusterCount; i++)
            {
                Seek(Constant.CLUSTER_AREA_POSITION + i * Header.ClusterSize);

                int status = reader.ReadInt32();

                // 사용되지 않더라고 카운팅 해줌
                if (status == 0)
                    status = 1;

                rClusterCount += status;
                i += (status - 1);
            }

            return (rClusterCount == this.ClusterCount);
        }
        #endregion

        #region 내부 함수
        /// <summary>
        /// 클러스터를 한칸 확장후 성공여부를 반환합니다.
        /// </summary>
        /// <param name="holder">확장할 클러스터 입니다.</param>
        /// <returns>클러스터 확장 결과입니다.</returns>
        internal override bool TryExpand(ClusterStreamHolder holder)
        {
            // 확장할 클러스터
            int idx = clusterStatus.FindIndex(c => c.Position == holder.Position - Constant.CLUSTER_SIZE);
            var data = clusterStatus[idx];

            // 할당할 클러스터
            int exIdx = idx + 1;// data.Used;

            if (exIdx >= clusterStatus.Count)
            {
                long totLength = GetStructureLength() + holder.BlockSize;

                if (BaseStream.Length < totLength)
                {
                    BaseStream.SetLength(totLength);
                }

                data.Used += 1;
                clusterStatus[idx] = data;

                holder.SetRange(holder.Position, (holder.Used + 1) * holder.BlockSize - Constant.CLUSTER_SIZE, false);
                holder.UpdateExpand();

                UpdateClusterCount();

                return true;
            }
            else
            {
                var exData = clusterStatus[exIdx];

                if (exData.Used == 0)
                {
                    data.Used += 1;
                    clusterStatus[idx] = data;

                    holder.SetRange(holder.Position, (holder.Used + 1) * holder.BlockSize - Constant.CLUSTER_SIZE, false);
                    holder.UpdateExpand();

                    // 할당된 클러스터영역 제거
                    clusterStatus.RemoveAt(exIdx);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 현재 스트림 내의 위치를 설정합니다.
        /// </summary>
        /// <param name="position">스트림 내의 새 위치입니다.</param>
        internal void Seek(long position)
        {
            BaseStream.Seek(position, SeekOrigin.Begin);
        }

        /// <summary>
        /// 사용가능한 클러스터 영역을 찾습니다.
        /// </summary>
        /// <param name="count">사용할 클러스터갯수입니다.</param>
        /// <returns>사용가능한 클러스터 인덱스입니다.</returns>
        internal int FindFreeClusterPosition(int count)
        {
            int c = 0;
            int fIdx = 0;

            for (int i = 0; i < clusterStatus.Count(); i++)
            {
                int used = clusterStatus.ElementAt(i).Used;

                if (used == 0)
                {
                    if (c++ == 0)
                        fIdx = i;
                }
                else
                    c = 0;

                if (c >= count)
                    return i - (count - 1);

                if (used == 0 && i == clusterStatus.Count() - 1)
                {
                    return fIdx;
                }
            }

            return -1;
        }

        // 해당 인덱스를 가진 클러스터로 이동
        private void MoveToCluster(int idx, Architecture architect)
        {
            long position;

            if (architect == Architecture.Physical)
            {
                if (idx >= ClusterCount)
                    throw new IndexOutOfRangeException();

                position =
                    Constant.CLUSTER_AREA_POSITION +
                    idx * Header.ClusterSize;
            }
            else
            {
                if (idx >= ClusterLength)
                    throw new IndexOutOfRangeException();

                position = clusterStatus[idx].Position;
            }

            SeekToCluster(idx, position);
        }

        private void SeekToCluster(int idx, long position)
        {
            Seek(position);
            int status = reader.ReadInt32();

            position += Constant.CLUSTER_EXPAND_SIZE;

            streamHolder.SetRange(idx, position, Header.ClusterSize * status - Constant.CLUSTER_SIZE, status);
        }

        private long GetStructureLength()
        {
            return Constant.CLUSTER_AREA_POSITION + GetClusterCount() * Header.ClusterSize;
        }

        // 물리적 클러스터 갯수 읽기
        private long GetClusterCount()
        {
            // Cluster Size Check
            if (BaseStream.Length < Constant.CLUSTER_AREA_POSITION)
                throw new ClusterNotFoundException("클러스터 영역을 찾을 수 없습니다.");

            long pos = BaseStream.Position;

            // Cluster Area Size Check
            Seek(Constant.CLUSTER_COUNT_POSITION);

            long result = reader.ReadInt64();

            Seek(pos);

            return result;
        }

        // 물리적 클러스터 갯수 업데이트
        private void UpdateClusterCount()
        {
            long areaSize = BaseStream.Length - Constant.CLUSTER_AREA_POSITION;
            long clusterCount = areaSize / Header.ClusterSize;

            long pos = BaseStream.Position;

            SetClusterCount(clusterCount);

            Seek(pos);
        }

        // 물리적 클러스터 갯수 설정
        private void SetClusterCount(long clusterCount)
        {
            this.ClusterCount = clusterCount;

            Seek(Constant.CLUSTER_COUNT_POSITION);
            writer.Write(clusterCount);
        }

        // 클러스터 영역 및 버퍼 생성
        private void CreateClusterArea(int clusterSize, int capacity)
        {
            BaseStream.SetLength(
                Constant.CLUSTER_AREA_POSITION +
                clusterSize * capacity);

            SetClusterCount(capacity);
        }

        // 포맷 헤더 생성
        private void CreateHeader(string version, int clusterSize, int clusterMaxExpand)
        {
            BaseStream.SetLength(Constant.HEADER_SIZE);

            streamHolder.SetRange(Constant.VERSION_POSITION, Constant.VERSION_SIZE);
            streamHolder.Write(version);

            streamHolder.SetRange(Constant.DATE_POSITION, Constant.DATE_SIZE);
            streamHolder.Write(DateTime.Now.ToBinary());

            streamHolder.SetRange(Constant.CLUSTER_POSITION, Constant.CLUSTER_SIZE);
            streamHolder.Write(clusterSize);

            streamHolder.SetRange(Constant.CLUSTER_EXPAND_POSITION, Constant.CLUSTER_EXPAND_SIZE);
            streamHolder.Write(clusterMaxExpand);
        }

        // 포맷 헤더 읽기
        private CFSHeader ReadHeader()
        {
            CFSHeader header = new CFSHeader();

            Seek(Constant.VERSION_POSITION);
            header.Version = reader.ReadString();

            Seek(Constant.DATE_POSITION);
            header.Date = reader.ReadInt64();

            Seek(Constant.CLUSTER_POSITION);
            header.ClusterSize = reader.ReadInt32();

            Seek(Constant.CLUSTER_EXPAND_POSITION);
            header.ClusterMaxExpand = reader.ReadInt32();

            return header;
        }

        // 포맷 구조 검사
        private void CheckStructure()
        {
            // Header Size Check
            if (BaseStream.Length < Constant.HEADER_SIZE)
                throw new HeaderNotFoundException("헤더 영역을 찾을 수 없습니다.");

            long clusterCount = GetClusterCount();

            Seek(Constant.CLUSTER_POSITION);
            int clusterSize = reader.ReadInt32();

            long areaSize = clusterCount * clusterSize;
            long streamLength =
                Constant.HEADER_SIZE +
                Constant.CLUSTER_COUNT_SIZE +
                areaSize;

            if (BaseStream.Length != streamLength)
                throw new ClusterNotFoundException("스트림이 클러스터 영역보다 짧습니다.");
        }
        #endregion

        #region Transaction
        /// <summary>
        /// 트랜잭션을 시작합니다.
        /// </summary>
        /// <returns>트랜잭션입니다.</returns>
        public CFSTransaction BeginTransaction()
        {
            if (this.transaction != null)
                throw new CFSException("Transaction is already open.");

            return transaction = CFSTransaction.OpenTransaction(this);
        }

        /// <summary>
        /// 트랜잭션을 종료합니다.
        /// </summary>
        public void EndTransaction()
        {
            if (transaction == null)
                throw new CFSException("Transaction is not open");

            this.transaction = null;
        }

        internal void Commit()
        {
            if (transaction == null)
                throw new CFSException("Transaction is not open");

            // 확장 및 사용중인 모든 클러스터 - Transaction
            int used = transaction.Clusters.Sum(c => c.Used);
            long tLength = used * Header.ClusterSize;

            // 사용 가능한 클러스터 - Stream
            int free = FindFreeClusterPosition(used); // useable position
            long cpPos = Constant.CLUSTER_AREA_POSITION + Header.ClusterSize * ClusterCount;

            long totLength = GetStructureLength() + tLength;

            if (free != -1)
            {
                cpPos = clusterStatus[free].Position;
                totLength = Math.Max(cpPos + tLength, this.BaseStream.Length);
            }
            else
            {
                // 동기화를 위해 맨 끝 클러스터인덱스로 이동
                free = clusterStatus.Count;
            }

            // 부분 동기화
            for (int i = 0; i < transaction.Clusters.Count; i++)
            {
                ClusterSurface c = transaction.Clusters[i];
                c.Position += cpPos - Constant.CLUSTER_SIZE;

                int cIdx = i + free;
                int lastIdx = Math.Min(i + free + c.Used, clusterStatus.Count) - 1;

                if (c.Used > 1 && lastIdx > cIdx)
                    clusterStatus.RemoveRange(cIdx + 1, lastIdx - cIdx);

                if (cIdx < clusterStatus.Count)
                {
                    clusterStatus[cIdx] = c;
                }
                else
                {
                    clusterStatus.Add(c);
                }
            }

            if (this.BaseStream.Length < totLength)
            {
                this.BaseStream.SetLength(totLength);
                UpdateClusterCount();
            }

            transaction.Stream.CopyTo(0, this.BaseStream, cpPos, tLength);
            transaction.Clear();
        }
        #endregion
    }
}