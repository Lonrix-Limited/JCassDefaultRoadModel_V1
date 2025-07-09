using JCass_ModelCore.Models;
using JCass_ModelCore.Utilities;
using JCassDefaultRoadModel.LookupObjects;
using JCassDefaultRoadModel.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.Initialisation;

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
        RoadSegment segment = RoadSegmentFactory.GetFromRawData(_frameworkModel, rawRow, _domainModel.LookupUtil);

        // Now do checks on the values and handle any anomalous data

        segment.AverageDailyTraffic = Math.Max(1, segment.AverageDailyTraffic); // Ensure ADT is at least 1
        segment.PavementAge = GetPavementAge(segment); // Calculate pavement age
        segment.SurfaceAge = GetSurfacingAge(segment); // Calculate surfacing age

        segment.RutParameterValue = GetInitialRuttingValue(segment); // Calculate initial rutting value

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
        paramValues["para_surf_class"] = segment.SurfaceClass;
        paramValues["para_surf_road_type"] = segment.SurfaceRoadType;
        paramValues["para_surf_cs_flag"] = segment.SurfaceIsChipSealFlag;


        paramValues["para_surf_cs_flag"] = segment.SurfaceIsChipSealFlag;
        paramValues["para_surf_cs_or_ac_flag"] = segment.SurfaceIsChipSealOrACFlag;
        paramValues["para_surf_mat"] = segment.SurfaceMaterial;


        paramValues["para_surf_func"] = segment.SurfaceFunction;
        paramValues["para_surf_layers"] = segment.SurfaceNumberOfLayers;
        paramValues["para_surf_thick"] = segment.SurfaceThickness;
        paramValues["para_surf_age"] = segment.SurfaceAge;
        paramValues["para_surf_exp_life"] = segment.SurfaceExpectedLife;

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
            return _domainModel.GetLookupValueNumber("rehab_resets_rut", segment.RoadType);
        }

        double ruttingRaw = Math.Max(segment.RutLwpMean85, segment.RutRwpMean85);

        // If segment has been resurfaced, determine the rutting exceedance and the reset
        bool hasBeenResurfaced = segment.SurfaceAge < surveyAge;
        if (hasBeenResurfaced)
        {
            double resetExceedenceThreshold = _domainModel.GetLookupValueNumber("reset_exceed_thresh_rut", segment.RoadType);
            double resetImprovementFactor = _domainModel.GetLookupValueNumber("reset_perc_improv_facts_rut ", segment.RoadType);
            
            double ruttingResetExceedence = 0;
            if (ruttingRaw > resetExceedenceThreshold) {ruttingResetExceedence = resetExceedenceThreshold - ruttingRaw; }

            double resetValue = ruttingRaw + ruttingResetExceedence * resetImprovementFactor;

        }

        // If segment has not been rehabilitated or resurfaced, use the raw rutting value
        return ruttingRaw;

    }

}
