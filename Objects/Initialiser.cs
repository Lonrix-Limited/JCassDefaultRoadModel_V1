using JCass_ModelCore.Models;
using JCass_ModelCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.Objects;

/// <summary>
/// Class to handle initialisation, including helper functions and some domain logic.
/// </summary>
public class Initialiser
{
    private ModelBase _frameworkModel;
    private RoadNetworkModel _domainModel;

    public Initialiser(ModelBase frameworkModel, RoadNetworkModel domainModel)
    {
        _frameworkModel = frameworkModel ?? throw new ArgumentNullException(nameof(frameworkModel), "Domain model cannot be null");
        _domainModel = domainModel ?? throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
    }

    public RoadSegment InitialiseSegment(string[] rawRow)
    {

        // Create a new RoadSegment object based purely on the raw data provided in the string array.
        RoadSegment segment = RoadSegmentFactory.GetFromRawData(_frameworkModel, rawRow);

        // Now do checks on the values and handle any anomalous data

        segment.AverageDailyTraffic = Math.Max(1, segment.AverageDailyTraffic); // Ensure ADT is at least 1
        segment.PavementAge = GetPavementAge(segment); 
        segment.SurfaceAge = GetSurfacingAge(segment); 
        
        segment.PctFlushing = _domainModel.FlushingModel.GetInitialValue(segment, segment.PctFlushing, _domainModel.Constants.BaseDate);
        segment.FlushingModelInfo = _domainModel.FlushingModel.GetCalibratedInitialSetupValues(segment, segment.PctFlushing);

        segment.PctEdgeBreaks = _domainModel.EdgeBreakModel.GetInitialValue(segment, segment.PctEdgeBreaks, _domainModel.Constants.BaseDate);
        segment.EdgeBreakModelInfo = _domainModel.EdgeBreakModel.GetCalibratedInitialSetupValues(segment, segment.PctEdgeBreaks);

        segment.PctScabbing = _domainModel.ScabbingModel.GetInitialValue(segment, segment.PctScabbing, _domainModel.Constants.BaseDate);
        segment.ScabbingModelInfo = _domainModel.ScabbingModel.GetCalibratedInitialSetupValues(segment, segment.PctScabbing);

        segment.PctLongTransCracks = _domainModel.LTCracksModel.GetInitialValue(segment, segment.PctLongTransCracks, _domainModel.Constants.BaseDate);
        segment.LTCracksModelInfo = _domainModel.LTCracksModel.GetCalibratedInitialSetupValues(segment, segment.PctLongTransCracks);

        segment.PctMeshCracks = _domainModel.MeshCrackModel.GetInitialValue(segment, segment.PctMeshCracks, _domainModel.Constants.BaseDate);
        segment.MeshCrackModelInfo = _domainModel.MeshCrackModel.GetCalibratedInitialSetupValues(segment, segment.PctMeshCracks);

        segment.PctShoving = _domainModel.ShovingModel.GetInitialValue(segment, segment.PctShoving, _domainModel.Constants.BaseDate);
        segment.ShovingModelInfo = _domainModel.ShovingModel.GetCalibratedInitialSetupValues(segment, segment.PctShoving);

        segment.PctPotholes = _domainModel.PotholeModel.GetInitialValue(segment, segment.PctPotholes, _domainModel.Constants.BaseDate);
        segment.PotholeModelInfo = _domainModel.PotholeModel.GetCalibratedInitialSetupValues(segment, segment.PctPotholes);

        segment.RutParameterValue = GetInitialRuttingValue(segment);
        segment.RutIncrement = GetRutIncrementEstimate(segment);

        segment.Naasra85 = GetInitialNaasraValue(segment);
        segment.NaasraIncrement = GetNaasraIncrementEstimate(segment);

        segment.GetPavementDistressIndex(_frameworkModel, _domainModel, 0);
        segment.GetSurfaceDistressIndex(_frameworkModel, _domainModel, 0);

        segment.GetObjectiveAreaUnderCurve(_frameworkModel, _domainModel, 0);


        return segment;
    }

    /// <summary>
    /// Creates a dictionary that holds a key for each model parameter and assigns the appropriate value from the segment object.
    /// </summary>
    /// <param name="segment">Road Segment object from which to get model parameter values</param>
    /// <returns>A dictionary with parameter names as keys and their corresponding values from the segment</returns>
    public Dictionary<string, object> GetParameterValues(RoadSegment segment)
    {
        Dictionary<string, object> paramValues = new Dictionary<string, object>();
        paramValues["para_adt"] = segment.AverageDailyTraffic;
        paramValues["para_hcv"] = segment.HeavyVehiclesPerDay;

        paramValues["para_pave_age"] = segment.PavementAge;
        paramValues["para_pave_remlife"] = segment.PavementRemainingLife;
        paramValues["para_pave_life_ach"] = segment.PavementAchievedLife;
        paramValues["para_hcv_risk"] = segment.HCVRisk;

        paramValues["para_surf_mat"] = segment.SurfaceMaterial;
        paramValues["para_surf_class"] = segment.SurfaceClass;
        paramValues["para_surf_cs_flag"] = segment.SurfaceIsChipSealFlag;
        paramValues["para_surf_cs_or_ac_flag"] = segment.SurfaceIsChipSealOrACFlag;
        paramValues["para_surf_road_type"] = segment.SurfaceRoadType;
        paramValues["para_surf_thick"] = segment.SurfaceThickness;
        paramValues["para_surf_layers"] = segment.SurfaceNumberOfLayers;
        paramValues["para_surf_func"] = segment.SurfaceFunction;
        paramValues["para_surf_exp_life"] = segment.SurfaceExpectedLife;
        paramValues["para_surf_age"] = segment.SurfaceAge;
        paramValues["para_surf_life_ach"] = segment.SurfaceAchievedLifePercent;
        paramValues["para_surf_remain_life"] = segment.SurfaceRemainingLife;

        paramValues["para_flush_pct"] = segment.PctFlushing;
        paramValues["para_flush_info"] = segment.FlushingModelInfo;

        paramValues["para_edgeb_pct"] = segment.PctEdgeBreaks;
        paramValues["para_edgeb_info"] = segment.EdgeBreakModelInfo;

        paramValues["para_scabb_pct"] = segment.PctScabbing;
        paramValues["para_scabb_info"] = segment.ScabbingModelInfo;

        paramValues["para_lt_cracks_pct"] = segment.PctLongTransCracks;
        paramValues["para_lt_cracks_info"] = segment.LTCracksModelInfo;

        paramValues["para_mesh_cracks_pct"] = segment.PctMeshCracks;
        paramValues["para_mesh_cracks_info"] = segment.MeshCrackModelInfo;

        paramValues["para_shove_pct"] = segment.PctShoving;
        paramValues["para_shove_info"] = segment.ShovingModelInfo;

        paramValues["para_poth_pct"] = segment.PctPotholes;
        paramValues["para_poth_info"] = segment.PotholeModelInfo;
        
        paramValues["para_rut_increm"] = segment.RutIncrement;
        paramValues["para_rut"] = segment.RutParameterValue;

        paramValues["para_naasra_increm"] = segment.NaasraIncrement;
        paramValues["para_naasra"] = segment.Naasra85;

        paramValues["para_sdi"] = segment.GetSurfaceDistressIndex(_frameworkModel, _domainModel, 0);
        paramValues["para_pdi"] = segment.GetPavementDistressIndex(_frameworkModel, _domainModel, 0);

        paramValues["para_obj_distress"] = segment.GetObjectiveDistress(_frameworkModel, _domainModel, 0);
        paramValues["para_obj_rsl"] = segment.GetObjectiveRemainingSurfaceLife(_frameworkModel, _domainModel);
        paramValues["para_obj_rutting"] = segment.GetObjectiveRutting(_frameworkModel, _domainModel);
        paramValues["para_obj_naasra"] = segment.GetObjectiveNaasra(_frameworkModel, _domainModel);
        paramValues["para_obj_o"] = segment.GetObjectiveValueRaw(_frameworkModel, _domainModel, 0);
        paramValues["para_obj"] = segment.GetObjectiveValue(_frameworkModel, _domainModel, 0);
        paramValues["para_obj_auc"] = segment.GetObjectiveAreaUnderCurve(_frameworkModel, _domainModel, 0);

        paramValues["para_maint_cost_perkm"] = segment.GetMaintenanceCostPerKm(_frameworkModel, _domainModel, 0);

        var csResult = CandidateSelector.EvaluateCandidate(segment, _frameworkModel, _domainModel, 0);
        paramValues["para_csl_status"] = csResult.Outcome;
        paramValues["para_csl_flag"] = csResult.IsValidCandidate ? 1 : 0; // 1 for valid candidate, 0 for invalid

        paramValues["para_is_treated_flag"] = segment.IsTreated; // Defaults to false initially
        paramValues["para_treat_count"] = segment.TreatmentCount; // Defaults to 0 initially

        // The following are Network Parameters - to be set automatically by the framework model:
        //para_pdi_rank
        //para_rut_rank
        //para_sdi_rank
        //para_sla_rank

        return paramValues;
    }

    
    private double GetPavementAge(RoadSegment segment)
    {
        try
        {
            DateTime pavDate = JCass_Core.Utils.HelperMethods.ParseDateNoTime(segment.PavementDateString);
            double age = (_domainModel.Constants.BaseDate - pavDate).TotalDays / 365.25; // Use 365.25 to account for leap years        
            if (age < 0)
            {
                _frameworkModel.LogMessage($"Pavement date for segment {segment.FeebackCode} is in the future", false);
            }
            return age;
        }
        catch(Exception ex)
        {
            throw new Exception($"Error calculating pavement age for segment {segment.FeebackCode}: {ex.Message}");
        }
    }

    private double GetSurfacingAge(RoadSegment segment)
    {
        try
        {
            DateTime surfDate = JCass_Core.Utils.HelperMethods.ParseDateNoTime(segment.SurfacingDateString);
            double age = (_domainModel.Constants.BaseDate - surfDate).TotalDays / 365.25; // Use 365.25 to account for leap years        
            if (age < 0)
            {
                _frameworkModel.LogMessage($"Surfacing date for segment {segment.FeebackCode} is in the future", false);
            }
            return Math.Max(age, 0.1);  //Ensure age is not zero to avoid division by zero errors
        }
        catch (Exception ex)
        {
            throw new Exception($"Error calculating surfacing age for segment {segment.FeebackCode}: {ex.Message}");
        }
    }

    private double GetHighSpeedSurveyAge(RoadSegment segment)
    {
        DateTime surveyDate = JCass_Core.Utils.HelperMethods.ParseDateNoTime(segment.HsdSurveyDateString);
        double age = (_domainModel.Constants.BaseDate - surveyDate).TotalDays / 365.25; // Use 365.25 to account for leap years        
        if (age < 0)
        {
            _frameworkModel.LogMessage($"HSD Survey date for segment {segment.FeebackCode} is in the future", false);
        }
        return age;
    }

    private double GetNaasraSurveyAge(RoadSegment segment)
    {
        DateTime surveyDate = JCass_Core.Utils.HelperMethods.ParseDateNoTime(segment.RoughnessSurveyDateString);
        double age = (_domainModel.Constants.BaseDate - surveyDate).TotalDays / 365.25; // Use 365.25 to account for leap years        
        if (age < 0)
        {
            _frameworkModel.LogMessage($"Roughness Survey date for segment {segment.FeebackCode} is in the future", false);
        }
        return age;
    }

    
    /// <summary>
    /// Get the initial rutting value, taking into account the HSD survey age and the Surfacing and Pavement ages. There are
    /// three possibilities:
    /// <para>1. The HSD survey is older than the Pavement Age: In this case we presume the segment has been rehabilitated
    /// after the survey and return the value in lookup set 'rehab_resets_rut' mapping to the segment's RoadType</para>
    /// <para>2. The HSD survey is not older than the Pavement Age but older than Surface Age: In this case we presume the 
    /// segment has been resurfaced after the survey and calculate the resetted value based on how much the raw rutting value 
    /// (calculated as the maximum of the LWP and RWP 85th percentile rut values) exceeds the reset exceedance threshold, and 
    /// return the resetted value using a formula</para>
    ///</para>
    /// <para>3. The HSD survey is not older than the Pavement Age or the Surface age - return the maximum of the LWP and RWP 85th percentile ruts    
    ///</para>
    /// </summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    private double GetInitialRuttingValue(RoadSegment segment)
    {
        double surveyAge = GetHighSpeedSurveyAge(segment);

        // If segment has been rehabilitated, return the lookup value for the rutting reset
        bool hasBeenRehabilitated = segment.PavementAge < surveyAge;
        if (hasBeenRehabilitated) {
            return _domainModel.GetLookupValueNumber("rehab_resets_rut", segment.SurfaceRoadType);
        }

        double ruttingRaw = Math.Max(segment.RutLwpMean85, segment.RutRwpMean85);

        // If segment has been resurfaced, determine the rutting exceedance and the reset
        bool hasBeenResurfaced = segment.SurfaceAge < surveyAge;
        if (hasBeenResurfaced)
        {
            double resetExceedenceThreshold = _domainModel.GetLookupValueNumber("reset_exceed_thresh_rut", segment.SurfaceRoadType);
            double resetImprovementFactor = _domainModel.GetLookupValueNumber("reset_perc_improv_facts_rut", segment.SurfaceRoadType);
            
            double ruttingResetExceedence = 0;
            if (ruttingRaw > resetExceedenceThreshold) {ruttingResetExceedence = resetExceedenceThreshold - ruttingRaw; }

            double resetValue = ruttingRaw + ruttingResetExceedence * resetImprovementFactor;
            return resetValue;
        }

        // If segment has not been rehabilitated or resurfaced, use the raw rutting value
        return ruttingRaw;

    }


    /// <summary>
    /// Get an estimate of the initial rut rate, in mm per year, based on the current rut value and the surface age. Here,
    /// we take into consideration that there is always some initial settlement. The effective rut rate is 
    /// based on the current rut minus the setting-in value (from lookup table) divided by the surface age. 
    /// A check is done to ensure the returned value is within a reasonable range (0.05 minimum to 1.5 maximum).
    /// </summary>    
    /// <returns>The estimated current rut rate, in mm/year</returns>
    private double GetRutIncrementEstimate(RoadSegment segment)
    {
        // Get the estimated "settling-in" rut depth from the lookup table
        double settingInRutDepth = _domainModel.GetLookupValueNumber("settling_in_values", "rut");

        // Get the rut increase after settlement. Ensure the value is not negative
        double rutAfterSettlement = Math.Max(0, segment.RutParameterValue - settingInRutDepth);

        double rutIncrementEstimate = rutAfterSettlement/segment.SurfaceAge;

        return Math.Clamp(rutIncrementEstimate, 0.05, 1.5);

    }

    /// <summary>
    /// Get the initial Naasra value, taking into account the Roughness survey age and the Surfacing and Pavement ages. There are
    /// three possibilities:
    /// <para>1. The HSD survey is older than the Pavement Age: In this case we presume the segment has been rehabilitated
    /// after the survey and return the value in lookup set 'rehab_resets_naasra' mapping to the segment's RoadType</para>
    /// <para>2. The HSD survey is not older than the Pavement Age but older than Surface Age: In this case we presume the 
    /// segment has been resurfaced after the survey and calculate the resetted value based on how much the raw Naasra value 
    /// (based on the 85th percentile Naasra value) exceeds the reset exceedance threshold, and 
    /// return the resetted value using a formula</para>
    ///</para>
    /// <para>3. The HSD survey is not older than the Pavement Age or the Surface age - return the 85th percentile Naasra value    
    ///</para>
    /// </summary>    
    /// <returns></returns>
    private double GetInitialNaasraValue(RoadSegment segment)
    {
        double surveyAge = GetNaasraSurveyAge(segment);

        // If segment has been rehabilitated, return the lookup value for the  reset
        bool hasBeenRehabilitated = segment.PavementAge < surveyAge;
        if (hasBeenRehabilitated)
        {
            return _domainModel.GetLookupValueNumber("rehab_resets_naasra", segment.SurfaceRoadType);
        }

        double naasraRaw = segment.Naasra85;

        // If segment has been resurfaced, determine the rutting exceedance and the reset
        bool hasBeenResurfaced = segment.SurfaceAge < surveyAge;
        if (hasBeenResurfaced)
        {
            double resetExceedenceThreshold = _domainModel.GetLookupValueNumber("reset_exceed_thresh_naasra", segment.SurfaceRoadType);
            double resetImprovementFactor = _domainModel.GetLookupValueNumber("reset_perc_improv_facts_naasra", segment.SurfaceRoadType);

            double naasraResetExceedence = 0;
            if (naasraRaw > resetExceedenceThreshold) { naasraResetExceedence = resetExceedenceThreshold - naasraRaw; }

            double resetValue = naasraRaw + naasraResetExceedence * resetImprovementFactor;
            return resetValue;

        }

        // If segment has not been rehabilitated or resurfaced, use the raw value
        return naasraRaw;

    }

    /// <summary>
    /// Get an estimate of the initial Naasra rate, in counts per year, based on the current Naasra value and the surface age. Here,
    /// we estimate (from lookup table) the initial post construction Naasra value based on Road Class code. The effective Naasra rate is 
    /// based on the current Naasra value minus the initial value divided by the surface age. 
    /// A check is done to ensure the returned value is within a reasonable range (0.2 minimum to 1.5 maximum).
    /// </summary>    
    /// <returns>The estimated current rut rate, in mm/year</returns>
    private double GetNaasraIncrementEstimate(RoadSegment segment)
    {
        // Get the estimated "settling-in" value depth from the lookup table
        double settingInValue= _domainModel.GetLookupValueNumber("settling_in_values", "naasra");

        // Get the rut increase after settlement. Ensure the value is not negative
        double naasrafterSettlement = Math.Max(0, segment.Naasra85 - settingInValue);

        double incrementEstimate = naasrafterSettlement / segment.SurfaceAge;

        return Math.Clamp(incrementEstimate, 0.2, 1.5);

    }


}
