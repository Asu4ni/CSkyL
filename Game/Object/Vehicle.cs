namespace CSkyL.Game.Object
{
    using CSkyL.Game.ID;
    using CSkyL.Transform;
    using CSkyL.UI;
    using System.Collections.Generic;
    using System.Linq;
    using Color = UnityEngine.Color;
    using Color32 = UnityEngine.Color32;

    public abstract class Vehicle : Object<VehicleID>, IObjectToFollow
    {
        public override string Name => manager.GetVehicleName(GetHeadVehicleID()._index);
        #region look ahead
        private Position _GetFrame(uint simulationFrame)
        {
            uint index = simulationFrame >> 4 & 3U;
            return _frames[index];
        }

        public void SimulationFrame()
        {
            ref var vehicle = ref GetVehicle();
            _frames[vehicle.m_lastFrame] = Position._FromVec(vehicle.m_targetPos2);
        }

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            ref var vehicle = ref GetVehicle();
            uint targetFrame = vehicle.GetTargetFrame(vehicle.Info, _vid);
            var hs = vehicle.Info.m_generatedInfo.m_size * 0.5f;

            for (int i = 0; i < 4; i++) {
                // target position
                uint targetF = (uint) (targetFrame - (16 * i));
                var colorT = new Color32(255, (byte) (100 + 50 * i), (byte) (64 * i), 255);
                OverlayUtil.RenderCircle(cameraInfo, _GetFrame(targetF), colorT, hs.x * (1 - .25f * i));
            }

            vehicle.GetSmoothPosition(_vid, out var pos, out var rot);
            Positioning positioning = new Positioning(Position._FromVec(pos), Angle._FromQuat(rot));
            
            var lookDir0 = positioning.angle.ToDisplacement(20);
            var lookPos0 = positioning.position.Move(lookDir0);
            OverlayUtil.RenderArrow(cameraInfo, positioning.position, lookPos0, Color.blue);

            var lookPos1 = GetSmoothLookPos();
            var lookDir1 = positioning.position.DisplacementTo(lookPos1);
            OverlayUtil.RenderArrow(cameraInfo, positioning.position, lookPos1, Color.red);

            if (lookDir1.Distance > .3f) {
                var lookDir = Displacement.Lerp(lookDir0, lookDir1, .9f);
                var lookPos = positioning.position.Move(lookDir);
                OverlayUtil.RenderArrow(cameraInfo, positioning.position, lookPos, Color.blue + Color.red);
            }
        }

        public Position GetSmoothLookPos()
        {
            ref var vehicle = ref GetVehicle();
            uint targetFrame = vehicle.GetTargetFrame(vehicle.Info, _vid);
            Position pos1 = _GetFrame(targetFrame - 1 * 16U);
            Position pos2 = _GetFrame(targetFrame - 0 * 16U);
            float t = ((targetFrame & 15U) + SimulationManager.instance.m_referenceTimer) * 0.0625f;
            return Position.Lerp(pos1, pos2, t);
        }
        #endregion

        public Positioning GetPositioning()
        {
            ref var vehicle = ref GetVehicle();
            vehicle.GetSmoothPosition(_vid, out var pos0, out var rot0);
            Positioning positioning = new Positioning(Position._FromVec(pos0), Angle._FromQuat(rot0));
            
            var lookPos1 = GetSmoothLookPos();
            var lookDir1 = positioning.position.DisplacementTo(lookPos1);
            if (lookDir1.Distance > .3f) {
                var lookDir0 = positioning.angle.ToDisplacement(1);
                var lookDir = Displacement.Lerp(lookDir0, lookDir1, .9f);
                positioning.angle = lookDir.AsLookingAngle();
            }
            return positioning;
        }

        public float GetSpeed() => GetVehicle().GetSmoothVelocity(_vid).magnitude;

        public virtual string GetStatus()
        {
            var vehicle = _Of(GetHeadVehicleID());
            var status = vehicle._VAI.GetLocalizedStatus(
                                vehicle._vid, ref vehicle.GetVehicle(), out var implID);
            switch (ObjectID._FromIID(implID)) {
            case BuildingID bid: status += " " + Building.GetName(bid); break;
            case HumanID hid: status += " " + Of(hid).Name; break;
            }
            return status;
        }
        public string GetPrefabName() => GetVehicle().Info.name;
        public Utils.Infos GetInfos()
        {
            var details = new Utils.Infos();

            var vehicle = _Of(GetHeadVehicleID());
            switch (vehicle.GetOwnerID()) {
            case BuildingID buildingID:
                details["Owner"] = Building.GetName(buildingID); break;
            case HumanID humanID:
                details["Owner"] = Of(humanID).Name; break;
            }

            vehicle._MoreDetails(ref details);
            return details;
        }
        public virtual void _MoreDetails(ref Utils.Infos details) { }

        public bool IsSpawned => _Is(global::Vehicle.Flags.Spawned);
        public bool IsReversed => _Is(global::Vehicle.Flags.Reversed);
        public bool IsHead => GetVehicle().m_leadingVehicle == 0;
        public bool IsEnd => GetVehicle().m_trailingVehicle == 0;
        public bool IsMiddle => !(IsHead || IsEnd);
        public bool IsFront => IsReversed ? IsEnd : IsHead;

        public float GetAttachOffsetFront() => _VInfo.m_attachOffsetFront;
        public VehicleID GetFrontVehicleID()
            => VehicleID._FromIndex(IsReversed ? GetVehicle().GetLastVehicle(_vid) :
                                               GetVehicle().GetFirstVehicle(_vid));

        public static VehicleID GetHeadVehicleIDof(VehicleID id)
            => VehicleID._FromIndex(_GetVehicle(id).GetFirstVehicle(id._index));
        public VehicleID GetHeadVehicleID()
            => VehicleID._FromIndex(GetVehicle().GetFirstVehicle(_vid));
        public ObjectID GetOwnerID()
            => ObjectID._FromIID(_VAI.GetOwnerID(_vid, ref GetVehicle()));
        public TransitID GetTransitLineID() => TransitID._FromIndex(GetVehicle().m_transportLine);
        // capacity == 0: invalid
        public void GetLoadAndCapacity(out int load, out int capacity)
            => _VAI.GetBufferStatus(_vid, ref GetVehicle(), out _, out load, out capacity);

        public static IEnumerable<Vehicle> GetIf(System.Func<Vehicle, bool> filter)
        {
            return Enumerable.Range(1, manager.m_vehicleCount)
                    .Select(i => Of(VehicleID._FromIndex((ushort) i)) as Vehicle)
                    .Where(v => v is Vehicle && filter(v));
        }

        internal static Vehicle _Of(VehicleID id)
        {
            var ai = _GetVehicle(GetHeadVehicleIDof(id)).Info.m_vehicleAI;
            switch (ai) {
            case BusAI busAi_________________: return new TransitVehicle(id, "Bus");
            case TramAI tramAi_______________: return new TransitVehicle(id, "Tram");
            case MetroTrainAI metroTrainAi___: return new TransitVehicle(id, "Metro");
            case PassengerTrainAI pTrainAi___: return new TransitVehicle(id, "Train");
            case PassengerPlaneAI pPlaneAi___: return new TransitVehicle(id, "Flight");
            case PassengerBlimpAI pBlimpAi___: return new TransitVehicle(id, "Blimp");
            case CableCarAI cableCarAi_______: return new TransitVehicle(id, "Gondola");
            case TrolleybusAI trolleybusAi___: return new TransitVehicle(id, "Trolleybus");
            case PassengerFerryAI pFerryAi___: return new TransitVehicle(id, "Ferry");
            case PassengerShipAI pShipAi_____: return new TransitVehicle(id, "Ship");
            case PassengerHelicopterAI phAi__: return new TransitVehicle(id, "Helicopter");

            case CargoTruckAI cargoTruckAi:
            case CargoTrainAI cargoTrainAi:
            case CargoShipAI cargoShipAi:
            case CargoPlaneAI cargoPlaneAi___: return new CargoVehicle(id);

            case AmbulanceAI ambulanceAi:
            case AmbulanceCopterAI aCopterAi_: return new ServiceVehicle(id, "Medical");
            case DisasterResponseVehicleAI dr:
            case DisasterResponseCopterAI drc: return new ServiceVehicle(id, "Disaster Response");
            case FireCopterAI fireCopterAi:
            case FireTruckAI fireTruckAi_____: return new ServiceVehicle(id, "Firefighting");
            case PoliceCopterAI pCopterAi:
            case PoliceCarAI policeCarAi_____: return new ServiceVehicle(id, "Police");
            case GarbageTruckAI gTruckAi_____: return new ServiceVehicle(id, "Garbage");
            case HearseAI hearseAi___________: return new ServiceVehicle(id, "Deathcare");
            case MaintenanceTruckAI mTruckAi_:
            case ParkMaintenanceVehicleAI pm_: return new ServiceVehicle(id, "Maintenance");
            case PostVanAI postVanAi_________: return new ServiceVehicle(id, "Postal");
            case SnowTruckAI snowTruckAi_____: return new ServiceVehicle(id, "Snow Plowing");
            case WaterTruckAI waterTruckAi___: return new ServiceVehicle(id, "Water Pumping");
            case TaxiAI taxiAi_______________: return new Taxi(id);

            case PrivatePlaneAI pPlaneAi:
            case PassengerCarAI pCarAi_______: return new PersonalVehicle(id);
            case BicycleAI bicycleAi_________: return new Bicycle(id);

            case BalloonAI balloonAi:
            case FishingBoatAI fishingBoatAi_: return new MissionVehicle(id);

            default:
                Log.Warn($"Vehicle(ID:{id} of type [{ai.GetType().Name}] is not recognized.");
                return null;
            }
        }

        protected ref global::Vehicle GetVehicle() => ref _GetVehicle(id);

        protected Vehicle(VehicleID id) : base(id)
        {
            _vid = id._index;
            _frames = new Position[4];
            ref var vehicle = ref _GetVehicle(id);
            for (int i = 0; i < 4; ++i) {
                _frames[i] = Position._FromVec(vehicle.m_targetPos2);
            }
        }
        private static ref global::Vehicle _GetVehicle(VehicleID id)
            => ref manager.m_vehicles.m_buffer[id._index];
        protected bool _Is(global::Vehicle.Flags flags) => (GetVehicle().m_flags & flags) != 0;
        protected bool _IsOfService(ItemClass.Service service)
            => GetVehicle().Info.GetService() == service;

        protected VehicleInfo _VInfo => GetVehicle().Info;
        protected VehicleAI _VAI => _VInfo.m_vehicleAI;

        protected ushort _vid;
        private Position[] _frames;


        private static readonly VehicleManager manager = VehicleManager.instance;
    }
}
