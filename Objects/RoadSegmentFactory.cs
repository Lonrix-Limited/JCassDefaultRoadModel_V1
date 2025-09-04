using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.EMMA;
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
    public static RoadSegment GetFromRawData(ModelBase model, string[] rawRow, int elementIndex)
    {
        RoadSegment segment = new RoadSegment();

        segment.ElementIndex = elementIndex; // Set the element index for this segment
        if (elementIndex == 123)
        {
            int kk = 9;
        }

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
        segment.IsRoundaboutFlag = Convert.ToBoolean(model.GetRawData_Text(rawRow, "file_is_roundabout_flag"));
        segment.CanTreatFlag = Convert.ToBoolean(model.GetRawData_Text(rawRow, "file_can_treat_flag"));
        segment.CanRehabFlag = Convert.ToBoolean(model.GetRawData_Text(rawRow, "file_can_rehab_flag"));
        segment.AsphaltOkFlag = Convert.ToBoolean(model.GetRawData_Text(rawRow, "file_ac_ok_flag"));
        segment.EarliestTreatmentPeriod = model.GetRawData_Number(rawRow, "file_earliest_treat_period");

        // Classification
        segment.UrbanRural = model.GetRawData_Text(rawRow, "file_urban_rural").ToLower();
        segment.ONRC = model.GetRawData_Text(rawRow, "file_onrc").ToLower();
        segment.NztaHierarchy = model.GetRawData_Text(rawRow, "file_nzta_hierarchy").ToLower();
        segment.OnfStreetCategory = model.GetRawData_Text(rawRow, "file_onf_street_category").ToLower();
        segment.OnfMovementRank = model.GetRawData_Text(rawRow, "file_onf_movement_rank").ToLower();
        segment.OnfFreight = model.GetRawData_Text(rawRow, "file_onf_freight").ToLower();
        
        //Lookup Road Class based on ONRC value (do NOTnuse file_road_class as this contains client-variant values)
        segment.RoadClass = model.GetLookupValueText("road_class", segment.ONRC);

        // Traffic        
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

    /// <summary>
    /// Gets a segment object from a model's input and parameter values dictionary. Use this method AFTER intialisation, when the model
    /// has already calculated initial values for model parameters and holds these values (initial or iterated/resetted). The inputAndParameterValues
    /// dictionary holds keys mapping to both the raw input columns and to the model parameters, with the Values mapping to the corresponding values.
    /// </summary>
    /// <param name="frameworkModel">Model object from which to refer the Raw Data schema</param>
    /// <param name="inputAndParameterValues">Dictionary provided by model containing all raw input values and parameter values with
    /// keys mapping to either raw input columns or to parameter names/codes and the Values mapping to the corresponding values./param>
    /// <returns></returns>
    public static RoadSegment GetFromModel(ModelBase frameworkModel, Dictionary<string, object> inputAndParameterValues, int elementIndex, int iPeriod)
    {
        RoadSegment segment = new RoadSegment();

        //First set all properties that are still dependend on the raw input data and that do not change over
        // the modelling periods

        segment.ElementIndex = elementIndex; // Set the element index for this segment

        // Identification
        segment.SegmentName = Convert.ToString(inputAndParameterValues["file_seg_name"]);
        segment.SectionID = Convert.ToInt32(inputAndParameterValues["file_section_id"]);
        segment.SectionName = Convert.ToString(inputAndParameterValues["file_section_name"]);
        segment.LocFrom = Convert.ToInt32(inputAndParameterValues["file_loc_from"]);
        segment.LocTo = Convert.ToInt32(inputAndParameterValues["file_loc_to"]);
        segment.LaneCode = Convert.ToString(inputAndParameterValues["file_lane_name"]);

        // Core measures
        segment.LengthInMetre = Convert.ToDouble(inputAndParameterValues["file_length"]);
        segment.AreaSquareMetre = Convert.ToDouble(inputAndParameterValues["file_area_m2"]);
        segment.WidthInMetre = segment.AreaSquareMetre / segment.LengthInMetre;

        // Flags
        segment.IsRoundaboutFlag = Convert.ToBoolean(inputAndParameterValues["file_is_roundabout_flag"]);
        segment.CanTreatFlag = Convert.ToBoolean(inputAndParameterValues["file_can_treat_flag"]);
        segment.CanRehabFlag = Convert.ToBoolean(inputAndParameterValues["file_can_rehab_flag"]);        
        segment.AsphaltOkFlag = Convert.ToBoolean(inputAndParameterValues["file_ac_ok_flag"]);
        segment.EarliestTreatmentPeriod = Convert.ToInt32(inputAndParameterValues["file_earliest_treat_period"]);

        // TODO: To discuss and make hardcoded value of 7 a lookup parameter
        if (iPeriod > 7) segment.CanRehabFlag = true; // For congruence with JFunction model, we allow rehab after 7 periods

        segment.AsphaltOkFlag = Convert.ToBoolean(inputAndParameterValues["file_ac_ok_flag"]);

        // Classification
        segment.UrbanRural = Convert.ToString(inputAndParameterValues["file_urban_rural"]).ToLower();
        segment.ONRC = Convert.ToString(inputAndParameterValues["file_onrc"]).ToLower();
        segment.NztaHierarchy = Convert.ToString(inputAndParameterValues["file_nzta_hierarchy"]).ToLower();
        segment.OnfStreetCategory = Convert.ToString(inputAndParameterValues["file_onf_street_category"]).ToLower();
        segment.OnfMovementRank = Convert.ToString(inputAndParameterValues["file_onf_movement_rank"]).ToLower();
        segment.OnfFreight = Convert.ToString(inputAndParameterValues["file_onf_freight"]).ToLower();
        
        //Lookup Road Class based on ONRC value (do NOTnuse file_road_class as this contains client-variant values)
        segment.RoadClass = frameworkModel.GetLookupValueText("road_class", segment.ONRC);

        // Traffic                
        segment.HeavyVehiclePercentage = Convert.ToDouble(inputAndParameterValues["file_heavy_perc"]);
        segment.NumberOfBusRoutes = Convert.ToDouble(inputAndParameterValues["file_no_of_bus_routes"]);
        segment.TrafficGrowthPercent = Convert.ToDouble(inputAndParameterValues["file_traff_growth_perc"]);

        // Surfacing
        segment.SurfaceClass = Convert.ToString(inputAndParameterValues["file_surf_class"]).ToLower();
        segment.NextSurface = Convert.ToString(inputAndParameterValues["file_next_surf"]);
        segment.SurfacingDateString = Convert.ToString(inputAndParameterValues["file_surf_date"]);
        

        // Pavement
        segment.PavementType = Convert.ToString(inputAndParameterValues["file_pave_type"]);
        segment.PavementDateString = Convert.ToString(inputAndParameterValues["file_pave_date"]);
        segment.PavementRemainingLife = Convert.ToDouble(inputAndParameterValues["file_pave_remlife"]);
        segment.FaultsAndMaintenanceSurfacingM2 = Convert.ToDouble(inputAndParameterValues["file_su_fault_qty"]);
        segment.FaultsAndMaintenancePavementM2 = Convert.ToDouble(inputAndParameterValues["file_pa_fault_qty"]);

        // Roughness and rutting
        segment.RoughnessSurveyDateString = Convert.ToString(inputAndParameterValues["file_roughsegment_date"]);        
        segment.HsdSurveyDateString = Convert.ToString(inputAndParameterValues["file_hsd_date"]);
        segment.RutLwpMean85 = Convert.ToDouble(inputAndParameterValues["file_rut_lwpmean_85"]);  // Original raw rutting value
        segment.RutRwpMean85 = Convert.ToDouble(inputAndParameterValues["file_rut_rwpmean_85"]);  // Original raw rutting value

        // Condition survey
        segment.ConditionSurveyDateString = Convert.ToString(inputAndParameterValues["file_cond_survey_date"]);

        // Now set the properties that depend on model parameters: Work in order of model parameter definition set
        // in the setup file so that we can more easily spot missing parameters.

        segment.AverageDailyTraffic = Convert.ToDouble(inputAndParameterValues["para_adt"]);
        // HCV is automatically updated based on ADT and HeavyVehiclePercentage
        segment.PavementAge = Convert.ToDouble(inputAndParameterValues["para_pave_age"]);
        segment.PavementRemainingLife = Convert.ToDouble(inputAndParameterValues["para_pave_remlife"]);
        // Note: segment.PavementAchievedLife will be automatically calculated by the model based on the PavementAge and PavementExpectedLife
        // Note segment.HCVRisk will be automatically calculated by the model based on the PavementUse and HeavyVehiclePercentage

        segment.SurfaceMaterial = Convert.ToString(inputAndParameterValues["para_surf_mat"]);
        segment.SurfaceClass = Convert.ToString(inputAndParameterValues["para_surf_class"]).ToLower();
        // Automatically updated:
        // segment.SurfaceIsChipSealFlag 
        // segment.SurfaceIsChipSealOrACFlag 
        // segment.SurfaceRoadType
        segment.SurfaceThickness = Convert.ToDouble(inputAndParameterValues["para_surf_thick"]);
        segment.SurfaceNumberOfLayers = Convert.ToDouble(inputAndParameterValues["para_surf_layers"]);
        segment.SurfaceFunction = Convert.ToString(inputAndParameterValues["para_surf_func"]);
        segment.SurfaceExpectedLife = Convert.ToDouble(inputAndParameterValues["para_surf_exp_life"]);
        segment.SurfaceAge = Convert.ToDouble(inputAndParameterValues["para_surf_age"]);         
        // Automatically updated:
        // segment.SurfaceAchievedLifePercent
        // segment.SurfaceRemainingLife 

        // Visual Distresses
        segment.PctFlushing = Convert.ToDouble(inputAndParameterValues["para_flush_pct"]);
        segment.FlushingModelInfo = Convert.ToString(inputAndParameterValues["para_flush_info"]);

        segment.PctEdgeBreaks = Convert.ToDouble(inputAndParameterValues["para_edgeb_pct"]);
        segment.EdgeBreakModelInfo = Convert.ToString(inputAndParameterValues["para_edgeb_info"]);

        segment.PctScabbing = Convert.ToDouble(inputAndParameterValues["para_scabb_pct"]);
        segment.ScabbingModelInfo = Convert.ToString(inputAndParameterValues["para_scabb_info"]);

        segment.PctLongTransCracks = Convert.ToDouble(inputAndParameterValues["para_lt_cracks_pct"]);
        segment.LTCracksModelInfo = Convert.ToString(inputAndParameterValues["para_lt_cracks_info"]);

        segment.PctMeshCracks = Convert.ToDouble(inputAndParameterValues["para_mesh_cracks_pct"]);
        segment.MeshCrackModelInfo = Convert.ToString(inputAndParameterValues["para_mesh_cracks_info"]);

        segment.PctShoving = Convert.ToDouble(inputAndParameterValues["para_shove_pct"]);
        segment.ShovingModelInfo = Convert.ToString(inputAndParameterValues["para_shove_info"]);

        segment.PctPotholes = Convert.ToDouble(inputAndParameterValues["para_poth_pct"]);
        segment.PotholeModelInfo = Convert.ToString(inputAndParameterValues["para_poth_info"]);

        //Rutting and Naasra
        segment.RutIncrement = Convert.ToDouble(inputAndParameterValues["para_rut_increm"]);  // Updated rut
        segment.RutParameterValue = Convert.ToDouble(inputAndParameterValues["para_rut"]);

        segment.NaasraIncrement = Convert.ToDouble(inputAndParameterValues["para_naasra_increm"]);  // Updated Naasra increment
        segment.Naasra85 = Convert.ToDouble(inputAndParameterValues["para_naasra"]);  // Updated Naasra value

        // Calculated values (to be calculated after this factor output returns):
        // para_sdi        
        //para_pdi
        //para_obj_distress
        //para_obj_rsl
        //para_obj_rutting
        //para_obj_naasra
        //para_obj_o
        //para_obj
        //para_obj_auc
        //para_maint_cost_perkm
        //para_csl_status
        //para_csl_flag

        segment.UpdateFormulaValuesFromParameters(inputAndParameterValues);

        segment.TreatmentCount = Convert.ToInt32(inputAndParameterValues["para_treat_count"]); // Will update IsTreated flag
        // para_is_treated_flag = automatically calculated based on treatment count

        segment.PavementDistressIndexRank = Convert.ToDouble(inputAndParameterValues["para_pdi_rank"]);
        segment.RutRank = Convert.ToInt32(inputAndParameterValues["para_rut_rank"]);
        segment.SurfaceDistressIndexRank = Convert.ToDouble(inputAndParameterValues["para_sdi_rank"]);
        segment.SurfaceLifeAchievedRank = Convert.ToDouble(inputAndParameterValues["para_sla_rank"]);

        // Ensure that the method to re-calculate index values are called on return

        return segment;
    }

}

