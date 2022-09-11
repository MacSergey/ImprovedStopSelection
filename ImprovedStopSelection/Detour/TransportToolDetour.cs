using System;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using UndergroundStopsEnabler.RedirectionFramework.Attributes;
using UnityEngine;

namespace ImprovedStopSelection.Detour
{
    [TargetType(typeof(TransportTool))]
    public class TransportToolDetour : TransportTool
    {
        [RedirectMethod]
        private bool GetStopPosition(TransportInfo info, ushort segment, ushort building, ushort firstStop, ref Vector3 hitPos, out bool fixedPlatform)
        {
            //begin mod(+): detect key
            bool alternateMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            //end mod

            NetManager instance = Singleton<NetManager>.instance;
            BuildingManager instance2 = Singleton<BuildingManager>.instance;
            TransportManager instance3 = Singleton<TransportManager>.instance;
            fixedPlatform = false;
            if (info.m_transportType == TransportInfo.TransportType.Pedestrian)
            {
                Vector3 position = Vector3.zero;
                float laneOffset = 0f;
                uint laneID = 0u;
                if (segment != 0 && !instance.m_segments.m_buffer[segment].GetClosestLanePosition(hitPos, NetInfo.LaneType.Pedestrian, VehicleInfo.VehicleType.None, VehicleInfo.VehicleCategory.None, VehicleInfo.VehicleType.None, out position, out laneID, out var _, out laneOffset))
                {
                    laneID = 0u;
                    if ((instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Untouchable) != 0 && building == 0)
                    {
                        building = NetSegment.FindOwnerBuilding(segment, 363f);
                    }
                }
                if (building != 0)
                {
                    BuildingInfo info2 = instance2.m_buildings.m_buffer[building].Info;
                    if (info2.m_hasPedestrianPaths)
                    {
                        laneID = instance2.m_buildings.m_buffer[building].FindLane(NetInfo.LaneType.Pedestrian, VehicleInfo.VehicleType.None, VehicleInfo.VehicleCategory.None, hitPos, out position, out laneOffset);
                    }
                    if (laneID == 0)
                    {
                        Vector3 sidewalkPosition = instance2.m_buildings.m_buffer[building].CalculateSidewalkPosition();
                        laneID = instance2.m_buildings.m_buffer[building].FindAccessLane(NetInfo.LaneType.Pedestrian, VehicleInfo.VehicleType.None, VehicleInfo.VehicleCategory.None, sidewalkPosition, out position, out laneOffset);
                    }
                }
                if (laneID != 0)
                {
                    if (laneOffset < 0.003921569f)
                    {
                        laneOffset = 0.003921569f;
                        position = instance.m_lanes.m_buffer[laneID].CalculatePosition(laneOffset);
                    }
                    else if (laneOffset > 0.996078432f)
                    {
                        laneOffset = 0.996078432f;
                        position = instance.m_lanes.m_buffer[laneID].CalculatePosition(laneOffset);
                    }
                    if (m_line != 0)
                    {
                        firstStop = instance3.m_lines.m_buffer[m_line].m_stops;
                        ushort num = firstStop;
                        int num2 = 0;
                        while (num != 0)
                        {
                            if (instance.m_nodes.m_buffer[num].m_lane == laneID)
                            {
                                hitPos = instance.m_nodes.m_buffer[num].m_position;
                                fixedPlatform = true;
                                return true;
                            }
                            num = TransportLine.GetNextStop(num);
                            if (num == firstStop)
                            {
                                break;
                            }
                            if (++num2 >= 32768)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                    }
                    hitPos = position;
                    fixedPlatform = true;
                    return true;
                }
                return false;
            }
            if (segment != 0)
            {
                if ((instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Untouchable) != 0)
                {
                    building = NetSegment.FindOwnerBuilding(segment, 363f);
                    if (building != 0)
                    {
                        BuildingInfo info3 = instance2.m_buildings.m_buffer[building].Info;
                        TransportInfo transportLineInfo = info3.m_buildingAI.GetTransportLineInfo();
                        TransportInfo secondaryTransportLineInfo = info3.m_buildingAI.GetSecondaryTransportLineInfo();
                        //begin mod(*): check for !alternateMode
                        if (!alternateMode && transportLineInfo != null && transportLineInfo.m_transportType == info.m_transportType || !alternateMode && secondaryTransportLineInfo != null && secondaryTransportLineInfo.m_transportType == info.m_transportType)
                        //end mod
                        {
                            segment = 0;
                        }
                        else
                        {
                            building = 0;
                        }
                    }
                }
                if (segment != 0 && instance.m_segments.m_buffer[segment].GetClosestLanePosition(hitPos, NetInfo.LaneType.Pedestrian, VehicleInfo.VehicleType.None, info.vehicleCategory, info.m_vehicleType, out var position2, out var laneID2, out var laneIndex2, out var _))
                {
                    if (info.m_vehicleType == VehicleInfo.VehicleType.None)
                    {
                        NetLane.Flags flags = (NetLane.Flags)instance.m_lanes.m_buffer[laneID2].m_flags;
                        flags &= NetLane.Flags.Stops;
                        NetLane.Flags flags2 = info.m_stopFlag;
                        NetInfo info4 = instance.m_segments.m_buffer[segment].Info;
                        if (info4.m_vehicleTypes != 0)
                        {
                            flags2 = NetLane.Flags.None;
                        }
                        if (flags != 0 && flags2 != 0 && flags != flags2)
                        {
                            return false;
                        }
                        NetInfo.Lane lane = info4.m_lanes[laneIndex2];
                        float num3 = lane.m_stopOffset;
                        if ((instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Invert) != 0)
                        {
                            num3 = 0f - num3;
                        }
                        instance.m_lanes.m_buffer[laneID2].CalculateStopPositionAndDirection(0.5019608f, num3, out hitPos, out var _);
                        fixedPlatform = true;
                        return true;
                    }
                    if (instance.m_segments.m_buffer[segment].GetClosestLanePosition(position2, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, info.m_vehicleType, info.vehicleCategory, out var _, out var laneID3, out var laneIndex3, out var _))
                    {
                        NetLane.Flags flags3 = (NetLane.Flags)instance.m_lanes.m_buffer[laneID2].m_flags;
                        flags3 &= NetLane.Flags.Stops;
                        if (flags3 != 0 && info.m_stopFlag != 0 && flags3 != info.m_stopFlag)
                        {
                            return false;
                        }
                        NetInfo.Lane lane2 = instance.m_segments.m_buffer[segment].Info.m_lanes[laneIndex3];
                        float num4 = lane2.m_stopOffset;
                        if ((instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Invert) != 0)
                        {
                            num4 = 0f - num4;
                        }
                        instance.m_lanes.m_buffer[laneID3].CalculateStopPositionAndDirection(0.5019608f, num4, out hitPos, out var _);
                        fixedPlatform = true;
                        return true;
                    }
                }
            }
            //begin mod(*): check for !alternateMode
            if (!alternateMode && building != 0)
            {
                //end mod
                ushort num5 = 0;
                if ((instance2.m_buildings.m_buffer[building].m_flags & Building.Flags.Untouchable) != 0)
                {
                    num5 = Building.FindParentBuilding(building);
                }
                if (m_building != 0 && firstStop != 0 && (m_building == building || m_building == num5))
                {
                    hitPos = instance.m_nodes.m_buffer[firstStop].m_position;
                    return true;
                }
                VehicleInfo randomVehicleInfo = Singleton<VehicleManager>.instance.GetRandomVehicleInfo(ref Singleton<SimulationManager>.instance.m_randomizer, info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level);
                if ((object)randomVehicleInfo != null)
                {
                    BuildingInfo info5 = instance2.m_buildings.m_buffer[building].Info;
                    TransportInfo transportLineInfo2 = info5.m_buildingAI.GetTransportLineInfo();
                    if ((object)transportLineInfo2 == null && num5 != 0)
                    {
                        building = num5;
                        info5 = instance2.m_buildings.m_buffer[building].Info;
                        transportLineInfo2 = info5.m_buildingAI.GetTransportLineInfo();
                    }
                    TransportInfo secondaryTransportLineInfo2 = info5.m_buildingAI.GetSecondaryTransportLineInfo();
                    if (((object)transportLineInfo2 != null && transportLineInfo2.m_transportType == info.m_transportType) || ((object)secondaryTransportLineInfo2 != null && secondaryTransportLineInfo2.m_transportType == info.m_transportType))
                    {
                        Vector3 vector = Vector3.zero;
                        int num6 = 1000000;
                        for (int i = 0; i < 12; i++)
                        {
                            Randomizer randomizer = new Randomizer((ulong)i);
                            info5.m_buildingAI.CalculateSpawnPosition(building, ref instance2.m_buildings.m_buffer[building], ref randomizer, randomVehicleInfo, out var position4, out var target);
                            int num7 = 0;
                            if (info.m_avoidSameStopPlatform)
                            {
                                num7 = GetLineCount(position4, target - position4, info.m_transportType);
                            }
                            if (info.m_transportType != TransportInfo.TransportType.Metro)
                            {
                                if (num7 < num6)
                                {
                                    vector = position4;
                                    num6 = num7;
                                }
                                else if (num7 == num6 && Vector3.SqrMagnitude(position4 - hitPos) < Vector3.SqrMagnitude(vector - hitPos))
                                {
                                    vector = position4;
                                }
                            }
                            else if (Vector3.SqrMagnitude(position4 - hitPos) < Vector3.SqrMagnitude(vector - hitPos))
                            {
                                vector = position4;
                                num6 = 0;
                            }
                        }
                        if (firstStop != 0)
                        {
                            Vector3 position5 = instance.m_nodes.m_buffer[firstStop].m_position;
                            if (Vector3.SqrMagnitude(position5 - vector) < 16384f)
                            {
                                uint lane3 = instance.m_nodes.m_buffer[firstStop].m_lane;
                                if (lane3 != 0)
                                {
                                    ushort segment2 = instance.m_lanes.m_buffer[lane3].m_segment;
                                    if (segment2 != 0 && (instance.m_segments.m_buffer[segment2].m_flags & NetSegment.Flags.Untouchable) != 0)
                                    {
                                        ushort num8 = NetSegment.FindOwnerBuilding(segment2, 363f);
                                        if (building == num8)
                                        {
                                            hitPos = position5;
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                        hitPos = vector;
                        return num6 != 1000000;
                    }
                }
            }
            return false;
        }

        [RedirectReverse]
        private int GetLineCount(Vector3 stopPosition, Vector3 stopDirection, TransportInfo.TransportType transportType)
        {
            UnityEngine.Debug.Log("GetLineCount");
            return 0;
        }

        private ushort m_line => (ushort)typeof(TransportTool).GetField("m_line",
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
    }
}
