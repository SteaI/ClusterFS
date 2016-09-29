# README #


### 피드백 ###
develope_e@naver.com


### Cluster File System ###
클러스터 시스템을 구현해 바이너리를 셀단위로 조작할 수 있는 파일 포맷입니다.


### 포맷 구조 ###
![cfs format.png](https://bitbucket.org/repo/9LX4j5/images/900675021-cfs%20format.png)


### 기본 예제 ###

**스트림 생성하기**
```
#!c#
var stream = new CFSStream(File.Create("db.cfs"), <포맷 버전>, <클러스터 크기>, <클러스터 확장 갯수>, <클러스터 버퍼>);

// ex
var stream = new CFSStream(File.Create("db.cfs"), "v1.0", 16, 4, 10);
```

**스트림 열기**
```
#!c#
var stream = new CFSStream(File.Open("db.cfs", FileMode.Open));
```

**파일 생성하기**
```
#!c#
var file = CFSFile.Create("db.cfs", "v1.0", 16, 4, 10);
```

**파일 열기**
```
#!c#
var file = CFSFile.Open("db.cfs");
```

**클러스터 생성 및 작성**

클러스터는 기본적으로 바이너리 읽기/쓰기를 동시에 지원합니다.
할당된 클러스터영역을 모두 사용했을 경우 확장을 시도하지만,
사용중인 클러스터 다음 클러스터가 비어있지 않다면 **EndOfStreamException**예외가 발생하게됩니다.
이를 해결하기위해 **CFSTransaction**부분을 보면 됩니다.
```
#!c#
ClusterStreamHolder cluster = file.CreateCluster();

cluster.Write("Hello CFS!");
```

**클러스터 읽기**
```
#!c#
// 부분 클러스터 읽기
var cluster = file.PeekCluster(<index>);
Console.WriteLine(cluster.ReadString());

// 모든 클러스터 읽기
foreach (var cluster in file.AllClusters())
{
    Console.WriteLine(cluster.ReadString());
}
```

**클러스터 삭제**
```
#!c#
file.RemoveCluster(<index>);
```

**클러스터 트랜잭션**
```
#!c#
using (file.BeginTransaction())
{
    for (int i = 0; i < 10000; i++)
    {
        var cluster = file.CreateCluster();
        cluster.Write($"Hello Transaction! {i}");
    }
}
```

**IClusterSerializer 예제**
```
#!c#
class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```
```
#!c#
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
        return new Person()
        {
            Name = holder.ReadString(),
            Age = holder.ReadInt32()
        };
    }

    public void Serialize(Person obj, ClusterStreamHolder holder)
    {
        holder.Write(obj.Name);
        holder.Write(obj.Age);
    }
}
```
```
#!c#
var ps = new PersonSerializer();
```
```
#!c#
using (file.BeginTransaction())
{
    for (int i = 0; i < 100; i++)
    {
        file.AddItem(new Person()
        {
            Name = "CFS",
            Age = i
        }, ps, false);
    }
}
```
```
#!c#
foreach (Person p in file.GetItems(ps))
{
    Console.WriteLine($"Name: {p.Name}, Age: {p.Age}");
}
```