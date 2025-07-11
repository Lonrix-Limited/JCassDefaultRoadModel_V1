using JCass_ModelCore.DomainModels;
using JCass_ModelCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.Objects;

public static class RoadSegmentFactory
{

    /// <summary>
    /// Creates a RoadSegment object from raw data provided in a string array. We assume columns are in the order defined in the model's raw data schema.
    /// </summary>
    /// <param name="model">Model object from which to refer the Raw Data schema</param>
    /// <param name="rawRow">Row of raw data values for each column in the schema</param>
    /// <returns></returns>
    public static RoadSegment GetFromRawData(ModelBase model, string[] rawRow)
    {
        RoadSegment segment = new RoadSegment();

        // Identification
        segment.SegmentName = model.GetRawData_Text(rawRow, "file_seg_name");
        segment.SectionID = model.GetRawData_Number(rawRow, "file_section_id");
        segment.SectionName = model.GetRawData_Text(rawRow, "file_section_name");
        segment.LocFrom = model.GetRawData_Number(rawRow, "file_loc_from");
        segment.LocTo = model.GetRawData_Number(rawRow, "file_loc_to");
        segment.LaneCode = model.GetRawData_Text(rawRow, "file_lane_name");

        // Core measures
        segment.LengthInMetre = model.GetRawData_Number(rawRow, "file_length");
        segment.AreaSquareMetre = model.GetRawData_Number(rawRow, "file_area_m2");
        segment.WidthInMetre = segment.AreaSquareMetre / segment.LengthInMetre;

        // Flags
        segment.IsRoundaboutFlag = model.GetRawData_Text(rawRow, "file_is_roundabout_flag");
        segment.CanTreatFlag = model.GetRawData_Text(rawRow, "file_can_treat_flag");
        segment.CanRehabFlag = model.GetRawData_Text(rawRow, "file_can_rehab_flag");
        segment.AsphaltOkFlag = model.GetRawData_Text(rawRow, "file_ac_ok_flag");
        segment.EarliestTreatmentPeriod = model.GetRawData_Number(rawRow, "file_earliest_treat_period");

        // Classification
        segment.UrbanRural = model.GetRawData_Text(rawRow, "file_urban_rural").ToLower();
        segment.ONRC = model.GetRawData_Text(rawRow, "file_onrc").ToLower();
        segment.NztaHierarchy = model.GetRawData_Text(rawRow, "file_nzta_hierarchy").ToLower();
        segment.OnfStreetCategory = model.GetRawData_Text(rawRow, "file_onf_street_category").ToLower();
        segment.OnfMovementRank = model.GetRawData_Text(rawRow, "file_onf_movement_rank").ToLower();
        segment.OnfFreight = model.GetRawData_Text(rawRow, "file_onf_freight").ToLower();
        segment.RoadUse = model.GetRawData_Text(rawRow, "file_road_use").ToLower();

        //Lookup Road Class based on ONRC value (do NOTnuse file_road_class as this contains client-variant values)
        segment.RoadClass = model.GetLookupValueText("road_class", segment.ONRC);

        // Traffic
        segment.NumberOfLanes = model.GetRawData_Number(rawRow, "file_no_of_lanes");
        segment.PavementUse = model.GetRawData_Number(rawRow, "file_pave_use");
        segment.AverageDailyTraffic = model.GetRawData_Number(rawRow, "file_adt");
        segment.HeavyVehiclePercentage = model.GetRawData_Number(rawRow, "file_heavy_perc");
        segment.NumberOfBusRoutes = model.GetRawData_Number(rawRow, "file_no_of_bus_routes");
        segment.TrafficGrowthPercent = model.GetRawData_Number(rawRow, "file_traff_growth_perc");

        // Surfacing
        segment.SurfaceClass = model.GetRawData_Text(rawRow, "file_surf_class").ToLower();
        segment.NextSurface = model.GetRawData_Text(rawRow, "file_next_surf");        
        segment.SurfacingDateString = model.GetRawData_Text(rawRow, "file_surf_date");
        segment.SurfaceFunction = model.GetRawData_Text(rawRow, "file_surf_function");
        segment.SurfaceMaterial = model.GetRawData_Text(rawRow, "file_surf_material");
        segment.SurfaceExpectedLife = model.GetRawData_Number(rawRow, "file_surf_life_expected");
        segment.SurfaceNumberOfLayers = model.GetRawData_Number(rawRow, "file_surf_layer_no");
        segment.SurfaceThickness = model.GetRawData_Number(rawRow, "file_surf_thick");

        // Pavement
        segment.PavementType = model.GetRawData_Text(rawRow, "file_pave_type");
        segment.PavementDateString = model.GetRawData_Text(rawRow, "file_pave_date");
        segment.PavementRemainingLife = model.GetRawData_Number(rawRow, "file_pave_remlife");
        segment.FaultsAndMaintenanceSurfacingM2 = model.GetRawData_Number(rawRow, "file_su_fault_qty");
        segment.FaultsAndMaintenancePavementM2 = model.GetRawData_Number(rawRow, "file_pa_fault_qty");

        // Roughness and rutting
        segment.RoughnessSurveyDateString = model.GetRawData_Text(rawRow, "file_roughsegment_date");
        segment.Naasra85 = model.GetRawData_Number(rawRow, "file_naasra_85");
        segment.HsdSurveyDateString = model.GetRawData_Text(rawRow, "file_hsd_date");
        segment.RutLwpMean85 = model.GetRawData_Number(rawRow, "file_rut_lwpmean_85");
        segment.RutRwpMean85 = model.GetRawData_Number(rawRow, "file_rut_rwpmean_85");

        // Condition survey
        segment.ConditionSurveyDateString = model.GetRawData_Text(rawRow, "file_cond_survey_date");

        // Condition percentages
        segment.PctMeshCracks = model.GetRawData_Number(rawRow, "file_pct_allig");
        segment.PctLongTransCracks = model.GetRawData_Number(rawRow, "file_pct_lt_crax");
        segment.PctPotholes = model.GetRawData_Number(rawRow, "file_pct_poth");
        segment.PctScabbing = model.GetRawData_Number(rawRow, "file_pct_scabb");
        segment.PctFlushing = model.GetRawData_Number(rawRow, "file_pct_flush");
        segment.PctShoving = model.GetRawData_Number(rawRow, "file_pct_shove");
        segment.PctEdgeBreaks = model.GetRawData_Number(rawRow, "file_pct_edgebreak");

        return segment;
    }
}

