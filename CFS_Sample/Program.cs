using CFS;
using CFS.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CFS_Sample
{
    class Program
    {
        public class NonSerialiazableTypeSurrogateSelector : ISerializationSurrogate, ISurrogateSelector
        {
            ISurrogateSelector _nextSelector;

            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                FieldInfo[] fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var fi in fieldInfos)
                {
                    if (IsKnownType(fi.FieldType))
                    {
                        info.AddValue(fi.Name, fi.GetValue(obj));
                    }
                    else
                        if (fi.FieldType.IsClass)
                    {
                        info.AddValue(fi.Name, fi.GetValue(obj));
                    }
                }
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                FieldInfo[] fieldInfos = obj.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var fi in fieldInfos)
                {
                    if (IsKnownType(fi.FieldType))
                    {
                        if (IsNullableType(fi.FieldType))
                        {
                            Type argumentValueForTheNullableType = GetFirstArgumentOfGenericType(fi.FieldType);
                            fi.SetValue(obj, info.GetValue(fi.Name, argumentValueForTheNullableType));
                        }
                        else
                        {
                            fi.SetValue(obj, info.GetValue(fi.Name, fi.FieldType));
                        }

                    }
                    else
                        if (fi.FieldType.IsClass)
                    {
                        fi.SetValue(obj, info.GetValue(fi.Name, fi.FieldType));
                    }
                }

                return obj;
            }

            private Type GetFirstArgumentOfGenericType(Type type)
            {
                return type.GetGenericArguments()[0];
            }

            private bool IsNullableType(Type type)
            {
                if (type.IsGenericType)
                    return type.GetGenericTypeDefinition() == typeof(Nullable<>);

                return false;
            }

            private bool IsKnownType(Type type)
            {
                return type == typeof(string) || type.IsPrimitive || type.IsSerializable;
            }

            public void ChainSelector(ISurrogateSelector selector)
            {
                this._nextSelector = selector;
            }

            public ISurrogateSelector GetNextSelector()
            {
                return _nextSelector;
            }

            public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
            {
                if (IsKnownType(type))
                {
                    selector = null;
                    return null;
                }
                else if (type.IsClass || type.IsValueType)
                {
                    selector = this;
                    return this;
                }
                else
                {
                    selector = null;
                    return null;
                }
            }
        }
        
        class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }

            public Person(string name, int age)
            {
                this.Name = name;
                this.Age = age;
            }

            public override string ToString()
            {
                return $"{Name} {Age}세";
            }
        }

        class PersonSerializer : IClusterSerializable<Person>
        {
            public bool CanDeserialize(ClusterStreamHolder holder)
            {
                return true;
            }

            public bool CanSerialize(Person obj)
            {
                return true;
            }

            public Person Deserialize(ClusterStreamHolder holder)
            {
                return new Person(holder.ReadString(), holder.ReadInt32());
            }

            public void Serialize(Person obj, ClusterStreamHolder holder)
            {
                holder.Write(obj.Name);
                holder.Write(obj.Age);
            }
        }

        static void Main(string[] args)
        {
            Test_Transaction_Speed();

            return;
            var file = CFSFile.Create("d.bin", "v1.0", 16, 4, 10);
            var ps = new PersonSerializer();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var t = file.BeginTransaction();

            for (int i = 0; i < 10000; i++)
            {
                file.AddItem(new Person($"소현섭 {i}", 20), ps, false);
                file.AddItem(new Person($"최진용 {i}", 19), ps, false);
            }

            t.Commit();

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);

            foreach (var p in file.GetItems(ps))
            {
                Console.WriteLine(p.ToString());
            }
        }

        private static void Test_Transaction_Speed()
        {
            var file = CFSFile.Create("d.bin", "v1.0", 16, 4, 10);
            
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            for (int i = 0; i < 10000; i++)
            {
                var c = file.CreateCluster();
                c.Write("Hello");
            }

            Console.WriteLine("No transaction");
            Console.WriteLine(sw.ElapsedMilliseconds.ToString() + "/ms");
            Console.WriteLine();

            sw.Reset();
            sw.Start();

            using (file.BeginTransaction())
            {
                for (int i = 0; i < 10000; i++)
                {
                    var c = file.CreateCluster();
                    c.Write("Hello");
                }
            }
            
            sw.Stop();


            Console.WriteLine("Transaction");
            Console.WriteLine(sw.ElapsedMilliseconds.ToString() + "/ms");
            Console.WriteLine();
        }

        private static void Test_NoTransaction()
        {
            var file = CFSFile.Create("d.bin", "v1.0", 16, 4, 10);
            
            var writeAll = new Action(() =>
            {
                foreach (var s in file.AllClusters(Architecture.Physical, true))
                {
                    Console.WriteLine($"{s.Index}. Used: {s.Used},  Data: {(s.Used > 0 ? s.ReadString() : "")}");
                }

                Console.WriteLine();
            });

            file.CreateCluster().Write("Hello World");
            file.CreateCluster().Write("Hello World");

            file.RemoveCluster(0);

            var t = file.BeginTransaction();
            file.CreateCluster().Write("Hello World2");
            t.Commit();

            writeAll();
        }

        private static void Test3()
        {
            var stream = new CFSStream(File.Create("d.bin"), "v1.0", 16, 4, 10);

            var transaction = stream.BeginTransaction();

            stream.CreateCluster().Write("테스트");
            transaction.Commit();

            stream.CreateCluster().Write("테스트22");
            transaction.Commit();

            stream.EndTransaction();

            Console.WriteLine(stream.PeekCluster(0).ReadString());
            Console.WriteLine(stream.PeekCluster(1).ReadString());

            transaction = stream.BeginTransaction();

            stream.PeekCluster(0).Write("테스트3");
            transaction.Commit();

            stream.EndTransaction();

            Console.WriteLine(stream.PeekCluster(0).ReadString());

            var transaction3 = stream.BeginTransaction();
            stream.PeekCluster(1).Write("테스트2cxz9789cxz879cxz879xzc987cxz987xcz987xz9c87asdasd456ad465asd654asda65ds65");
            transaction3.Commit();

            Console.WriteLine(stream.PeekCluster(1).ReadString());

            //Test_Transaction();

            Console.ReadLine();
        }

        /// <summary>
        /// 클러스터 추가/삭제 테스트
        /// </summary>
        static void Test_Transaction()
        {
            var stream = new CFSStream(File.Create("d.bin"), "v1.0", 16, 4, 10);

            var addAction = new Action<int, string>((count, content) =>
            {
                var transaction = stream.BeginTransaction();
                for (int i = 0; i < count; i++)
                {
                    var c = stream.CreateCluster();
                    c.Write(content);
                }
                transaction.Commit();
                stream.EndTransaction();
            });

            var writeAll = new Action(() =>
            {
                foreach (var s in stream.AllClusters(Architecture.Physical, true))
                {
                    Console.WriteLine($"{s.Index}. Used: {s.Used},  Data: {(s.Used > 0 ? s.ReadString() : "")}");
                }

                Console.WriteLine();
            });

            Console.WriteLine(" < Write 3 Clusters >");
            addAction.Invoke(3, "Hello World !!");
            writeAll();

            Console.WriteLine(" < RemoveCluster 1 >");
            stream.RemoveCluster(1);
            writeAll();

            Console.WriteLine(" < Write 1 Cluster >");
            addAction.Invoke(2, "헬로!");
            writeAll();

            Console.WriteLine(" < Write Big Cluster >");
            addAction.Invoke(2, "Hello World !!!!!!!!!!!!!!!!!");
            writeAll();

            Console.WriteLine(" < RemoveCluster 4 >");
            stream.RemoveCluster(4);
            writeAll();

            Console.WriteLine(" < Write 1 Cluster >");
            addAction.Invoke(1, "헬로2!");
            writeAll();

            stream.Close();
        }

        /// <summary>
        /// 스트림 생성 테스트
        /// </summary>
        static void Test_Stream()
        {
            var stream = new CFSStream(File.Create("d.bin"), "v1.0", 200, 4, 10);
            Console.WriteLine("Created");

            Console.WriteLine($"\r\n < CFS Header >");
            Console.WriteLine($"Version: {stream.Header.Version}");
            Console.WriteLine($"ClusterSize: {stream.Header.ClusterSize}");
            Console.WriteLine($"ClusterMaxExpand: {stream.Header.ClusterMaxExpand}");
            Console.WriteLine($"CreateDate: {DateTime.FromBinary(stream.Header.Date)}");

            // Write 197 bytes 4 times
            var transaction = stream.BeginTransaction();

            for (int i = 0; i < 4; i++)
            {
                var c = stream.CreateCluster();
                c.Write(new byte[197]);
            }

            transaction.Commit();

            Console.WriteLine("\r\n < Index based - Physical >");
            for (int i = 0; i < stream.ClusterCount; i++)
            {
                var s = stream.PeekCluster(i, Architecture.Physical);

                Console.WriteLine($"{i}. Used: {s.Used}");

                i += Math.Max(s.Used - 1, 0);
            }

            Console.WriteLine("\r\n < Index based - Logical >");
            for (int i = 0; i < stream.ClusterLength; i++)
            {
                var s = stream.PeekCluster(i);

                Console.WriteLine($"{i}. Used: {s.Used}");
            }

            Console.WriteLine("\r\n < AllClusters - Physical >");
            foreach (var s in stream.AllClusters(Architecture.Physical))
            {
                Console.WriteLine($"{s.Index}. Used: {s.Used}");
            }

            Console.WriteLine("\r\n < AllClusters - Logical >");
            foreach (var s in stream.AllClusters(Architecture.Logical))
            {
                Console.WriteLine($"{s.Index}. Used: {s.Used}");
            }

            Console.WriteLine("\r\n < AllClusters - Physical, all >");
            foreach (var s in stream.AllClusters(Architecture.Physical, true))
            {
                Console.WriteLine($"{s.Index}. Used: {s.Used}");
            }

            Console.WriteLine("\r\n < AllClusters - Logical, all >");
            foreach (var s in stream.AllClusters(Architecture.Logical, true))
            {
                Console.WriteLine($"{s.Index}. Used: {s.Used}");
            }

            stream.Close();
        }
    }
}
