namespace CSkyL.Game.Object
{
    using CSkyL.Game.ID;

    public interface IObject
    {
        string Name { get; }
        ObjectID ID { get; }
    }
    public interface IObjectToFollow : IObject
    {
        Transform.Position GetTargetPos(int index);
        byte GetLastFrame();
        uint GetTargetFrame();
        float GetSpeed();
        Transform.Positioning GetPositioning();
        Utils.Infos GetInfos();
        string GetStatus();
        string GetPrefabName();
    }

    public abstract class Object : IObject
    {
        public abstract string Name { get; }
        public abstract ObjectID ID { get; }

        public static Object Of(ObjectID id)
        {
            if (!(id?.IsValid ?? false)) return null;
            switch (id) {
            case HumanID hid: return Human._Of(hid);
            case PedestrianID pid: return Pedestrian._Of(pid);
            case VehicleID vid: return Vehicle._Of(vid);
            case BuildingID bid: return Building._Of(bid);
            default: return null;
            }
        }
    }

    public abstract class Object<IDType> : Object where IDType : ObjectID
    {
        public override ObjectID ID => id;
        public override string Name {
            get {
                var name = InstanceManager.instance.GetName(id._iID);
                return string.IsNullOrEmpty(name) ? "(unknown)" : name;
            }
        }
        protected Object(IDType id) { this.id = id; }

        public readonly IDType id;
    }

    public class Building : Object<BuildingID>
    {
        public override string Name => GetName(id);
        public static string GetName(BuildingID id)
            => BuildingManager.instance.GetBuildingName(id._index, id._iID);

        internal static Building _Of(BuildingID id) => new Building(id);
        private Building(BuildingID id) : base(id) { }
    }
    public class TransitLine : Object<TransitID>
    {
        public override string Name => GetName(id);
        public static string GetName(TransitID id)
            => TransportManager.instance.GetLineName(id._index);

        internal static TransitLine _Of(TransitID id) => new TransitLine(id);
        private TransitLine(TransitID id) : base(id) { }
    }
    public class Node : Object<NodeID>
    {
        public TransitID TransitLineID => GetTransitLineID(id);
        public static TransitID GetTransitLineID(NodeID id)
            => TransitID._FromIndex(NetManager.instance.m_nodes
                            .m_buffer[id._index].m_transportLine);

        internal static Node _Of(NodeID id) => new Node(id);
        private Node(NodeID id) : base(id) { }
    }
    public class Segment : Object<SegmentID>
    {
        public override string Name => GetName(id);
        public static string GetName(SegmentID id)
            => NetManager.instance.GetSegmentName(id._index);

        internal static Segment _Of(SegmentID id) => new Segment(id);
        private Segment(SegmentID id) : base(id) { }
    }
    public class District : Object<DistrictID>
    {
        public override string Name => GetName(id);
        public static string GetName(DistrictID id)
            => DistrictManager.instance.GetDistrictName(id._index);

        internal static District _Of(DistrictID id) => new District(id);
        private District(DistrictID id) : base(id) { }
    }
}
