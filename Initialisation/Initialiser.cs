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
        segment.SurfacingAge = GetSurfacingAge(segment); // Calculate surfacing age

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
        paramValues["para_surf_age"] = segment.SurfacingAge;
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

}
