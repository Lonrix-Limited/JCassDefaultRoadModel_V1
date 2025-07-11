using JCass_ModelCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.Objects;

/// <summary>
/// Static class to help with calculation of Indexes (PDI, SDI, etc.) and Objective Functions.
/// </summary>
public static class CalculationUtilities
{

    /// <summary>
    /// Utility function to calculate the Logit function on a value, where the function is
    /// defined as 'Math.Exp(value) / (1 + Math.Exp(value))'
    /// </summary>
    /// <param name="value">Value on which to calculate logic</param>
    /// <returns></returns>
    public static double Logit(double value)
    {
        return Math.Exp(value) / (1 + Math.Exp(value));
    }

    /// <summary>
    /// Calculates the Pavement Distress Index (PDI) for a road segment based on the current period. Short term includes maintenance and faults
    /// </summary>    
    /// <param name="currentPeriod">Current modelling period (e.g. 1,2,3...) used to determine whether we are in short or long term</param>
    /// <returns></returns>
    public static double GetPavementDistressIndex(RoadSegment segment, ModelBase frameworkModel, RoadNetworkModel roadModel, int currentPeriod)
    {
        double boostedPotholes = segment.PctPotholes * roadModel.Constants.PotholeBoostFactor;
        bool isShortTerm = roadModel.Constants.CSShortTermPeriod <= currentPeriod;
        if (isShortTerm)
        {
            return GetPavementDistressIndexShortTerm(segment, frameworkModel, boostedPotholes);
        }
        else
        {
            return GetPavementDistressIndexLongTerm(segment, frameworkModel, boostedPotholes);
        }
    }

    /// <summary>
    /// Calculates the Surface Distress Index (SDI) for a road segment based on the current period. Short term includes maintenance and faults
    /// </summary>    
    /// <param name="currentPeriod">Current modelling period (e.g. 1,2,3,...) used to determine whether we are in short or long term</param>
    /// <returns></returns>
    public static double GetSurfacingDistressIndex(RoadSegment segment, ModelBase frameworkModel, RoadNetworkModel roadModel, int currentPeriod)
    {
        double boostedPotholes = segment.PctPotholes * roadModel.Constants.PotholeBoostFactor;
        bool isShortTerm = roadModel.Constants.CSShortTermPeriod <= currentPeriod;
        if (isShortTerm)
        {
            return GetSurfacingDistressIndexShortTerm(segment, frameworkModel, boostedPotholes);
        }
        else
        {
            return GetSurfacingDistressIndexLongTerm(segment, frameworkModel, boostedPotholes);
        }
    }

    /// <summary>
    /// Calcuates the Pavement Distress Index (PDI) for a road segment based. Short term includes maintenance and faults 
    /// percentage.
    /// </summary>
    /// <param name="segment"></param>
    /// <param name="frameworkModel"></param>    
    private static double GetPavementDistressIndexShortTerm(RoadSegment segment, ModelBase frameworkModel, double boostedPotholes)
    {     
        double value = 0.2 * segment.PctLongTransCracks 
            + segment.PctMeshCracks 
            + segment.PctShoving 
            + boostedPotholes 
            + segment.FaultsAndMaintenancePavementPercent;

        return value;            
    }

    /// <summary>
    /// Calculates the Pavement Distress Index (PDI) for a road segment based on long-term distress measures. Long term 
    /// currently also includes maintenance and faults to prevent a sudden drop in PDI after the short term period is over.
    /// TODO: To discuss the inclusion of maintenance and faults in the long term PDI.
    /// </summary>    
    private static double GetPavementDistressIndexLongTerm(RoadSegment segment, ModelBase frameworkModel, double boostedPotholes)
    {        
        double value = 0.2 * segment.PctLongTransCracks
            + segment.PctMeshCracks
            + segment.PctShoving
            + boostedPotholes
            + segment.FaultsAndMaintenancePavementPercent;

        return value;
    }

    /// <summary>
    /// Calculates the Surface Distress Index (SDI) for a road segment based on short-term distress measures. Short term includes maintenance and faults 
    /// percentage.
    /// </summary>    
    private static double GetSurfacingDistressIndexShortTerm(RoadSegment segment, ModelBase frameworkModel, double boostedPotholes)
    {        
        
        double value = segment.PctFlushing + segment.PctScabbing + 0.5*segment.PctLongTransCracks + boostedPotholes + segment.FaultsAndMaintenanceSurfacingPercent;        
        return value;
    }

    /// <summary>
    /// Calculates the Surface Distress Index (PDI) for a road segment based on long-term distress measures. Long term 
    /// currently also includes maintenance and faults to prevent a sudden drop in PDI after the short term period is over.
    /// TODO: To discuss the inclusion of maintenance and faults in the long term PDI.
    /// </summary>  
    private static double GetSurfacingDistressIndexLongTerm(RoadSegment segment, ModelBase frameworkModel, double boostedPotholes)
    {
        double value = segment.PctFlushing + segment.PctScabbing + 0.5 * segment.PctLongTransCracks + boostedPotholes + segment.FaultsAndMaintenanceSurfacingPercent;
        return value;
    }



}
