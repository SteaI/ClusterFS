using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClusterFS.IO
{
    /// <summary>
    /// 포맷에 대한 클러스터 생성, 삭제 및 읽기를 제공하는 <see cref="CFSFile"/> 개체 만들기를 지원합니다.
    /// </summary>
    public class CFSFile : CFSStream
    {
        internal CFSFile(FileStream stream) : base(stream)
        {
        }

        internal CFSFile(FileStream stream, string version, int clusterSize, int clusterMaxExpand, int capacity) : base(stream, version, clusterSize, clusterMaxExpand, capacity)
        {
        }

        public IEnumerable<T> GetItems<T>(IClusterSerializable<T> serializer)
        {
            return base.AllClusters()
                .Where(c => serializer.CanDeserialize(c))
                .Select(c => serializer.Deserialize(c));
        }

        public bool AddItem<T>(T item, IClusterSerializable<T> serializer, bool useTransaction = true)
        {
            if (serializer.CanSerialize(item))
            {
                CFSTransaction t = null;

                if (useTransaction)
                    t = BeginTransaction();

                serializer.Serialize(item, this.CreateCluster());

                if (useTransaction)
                {
                    t.Commit();
                    EndTransaction();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 지정된 경로에서 읽기/쓰기 권한으로 <see cref="CFSFile"/>을 엽니다.
        /// </summary>
        /// <param name="filename">열 파일입니다.</param>
        /// <returns></returns>
        public static CFSFile Open(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException();
            
            return new CFSFile(File.Open(filename, FileMode.Open));
        }


        /// <summary>
        /// 지정된 경로에서 읽기/쓰기 권한으로 <see cref="CFSFile"/>을 엽니다. 반환값은 열기의 성공여부를 반환합니다.
        /// </summary>
        /// <param name="filename">열 파일입니다.</param>
        /// <param name="cFile">이 메서드는 파일 열기에 성공한 경우 <see cref="CFSFile"/>을 반환하고, 파일 열기에 실패한 경우 null을 반환합니다.
        ///     <para><paramref name="filename"/> 매개 변수가 null이거나, 파일이름이 올바르지 않거나,</para>
        ///     <para>파일이 존재하지 않는경우 열기에 실패합니다. 이 매개 변수는 초기화되지 않은 상태로 전달됩니다.</para></param>
        /// <returns></returns>
        public static bool TryOpen(string filename, out CFSFile cFile)
        {
            cFile = null;

            try
            {
                cFile = Open(filename);

                return true;
            }
            catch (FileNotFoundException)
            {
            }

            return false;
        }

        /// <summary>
        /// 지정된 경로에서 읽기/쓰기 권한으로 <see cref="CFSFile"/> 포맷 클래스의 새 파일을 생성합니다.
        /// </summary>
        /// <param name="filename">만들 파일의 경로와 이름입니다.</param>
        /// <param name="version">CFS파일 버전입니다.</param>
        /// <param name="clusterSize">클러스터 한 블럭의 크기입니다.</param>
        /// <param name="clusterMaxExpand">확장 가능한 최대 클러스터 갯수 입니다.</param>
        /// <param name="capacity">클러스터의 제안된 시작 갯수 입니다.</param>
        /// <returns></returns>
        public static CFSFile Create(
            string filename, string version, 
            int clusterSize = 256, 
            int clusterMaxExpand = 8, 
            int capacity = 16)
        {
            return new CFSFile(File.Create(filename), version, clusterSize, clusterMaxExpand, capacity);
        }
    }
}
