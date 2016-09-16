namespace ClusterFS.IO
{
    public interface IClusterSerializable<T>
    {
        bool CanSerialize(T obj);
        bool CanDeserialize(ClusterStreamHolder holder);

        void Serialize(T obj, ClusterStreamHolder holder);
        T Deserialize(ClusterStreamHolder holder);
    }
}