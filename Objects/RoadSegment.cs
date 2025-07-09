using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2013.Excel;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using JCass_Core.Engineering;
using JCass_ModelCore.Treatments;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JCassDefaultRoadModel.Objects;


/// <summary>
/// Object representing a road segment with various properties and attributes.
/// </summary>
public class RoadSegment
{

    #region Identification

    /// <summary>
    /// Zero-based index of the element in the model. This is set by the Framework Model and is used to identify the element in the model.
    /// </summary>
    public int ElementIndex { get; set; }

    /// <summary>
    /// Short code for identifying the segment in debug/feeback messages
    /// </summary>
    public string FeebackCode
    {
        get
        {
            return $"elem_index: {this.ElementIndex:D4} - {this.SegmentName}";
        }
    }

    /// <summary>
    /// Segment identifier. Maps to input column "file_seg_name".
    /// </summary>
    public string SegmentName { get; set; }

    /// <summary>
    /// Section ID. Maps to "file_section_id".
    /// </summary>
    public double SectionID { get; set; }

    /// <summary>
    /// Name of the section. Maps to "file_section_name".
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Start metre of the segment. Maps to "file_loc_from".
    /// </summary>
    public double LocFrom { get; set; }

    /// <summary>
    /// End metre of the segment. Maps to "file_loc_to".
    /// </summary>
    public double LocTo { get; set; }

    /// <summary>
    /// Lane code. Maps to "file_lane_name".
    /// </summary>
    public string LaneCode { get; set; }

    #endregion

    #region Quantity 

    /// <summary>
    /// Length of the segment in metres.
    /// </summary>
    public double LengthInMetre { get; set; }

    /// <summary>
    /// Square metre area.
    /// </summary>
    public double AreaSquareMetre { get; set; }

    /// <summary>
    /// Width in metres. By default, this is calculated on initialisation from Area and Length
    /// </summary>
    public double WidthInMetre { get; set; }

    #endregion

    #region Situational and Treatment Flags

    /// <summary>
    /// Indicates if the segment is a roundabout (not in use).
    /// </summary>
    public string IsRoundaboutFlag { get; set; }

    /// <summary>
    /// Can this segment be considered for treatment (client specific based on policy).
    /// </summary>
    public string CanTreatFlag { get; set; }

    /// <summary>
    /// Can this segment be considered for Rehab (client specific).
    /// </summary>
    public string CanRehabFlag { get; set; }

    /// <summary>
    /// Is the pavement suitable for asphalt resurfacing.
    /// </summary>
    public string AsphaltOkFlag { get; set; }

    /// <summary>
    /// Earliest modelling period the first treatment may be triggered.
    /// </summary>
    public double EarliestTreatmentPeriod { get; set; }

    #endregion
    
    #region Surface and Pavement Properties

    /// <summary>
    /// Surface class ('cs', 'ac', 'blocks', 'concrete', 'other').
    /// </summary>
    public string SurfaceClass { get; set; }

    /// <summary>
    /// Code that determines exceedance thresholds and improvement factors based on Urban/Rural, Road Class, and Surf Class. This code
    /// is a concatenation of SurfaceClass and RoadType using an underscore to delimit.
    /// </summary>    
    public string SurfaceRoadType
    {
        get
        {
            return this.SurfaceClass + "_" + this.RoadType;
        }
    }

    /// <summary>
    /// Flag indicating if the surface is a chip seal. This is calculated based on the SurfaceClass property.
    /// </summary>
    public int SurfaceIsChipSealFlag
    {
        get
        {
            // Return 1 if the surface class is 'cs' (chip seal), otherwise return 0.
            return this.SurfaceClass == "cs" ? 1 : 0;
        }
    }

    /// <summary>
    /// Flag indicating if the surface is either chip seal or asphalt concrete. This is calculated based on the SurfaceClass property.
    /// </summary>
    public int SurfaceIsChipSealOrACFlag
    {
        get
        {
            // Return 1 if the surface class is 'cs' (chip seal) or 'ac' (asphalt concrete), otherwise return 0.
            return this.SurfaceClass == "cs" || this.SurfaceClass == "ac" ? 1 : 0;
        }
    }

    /// <summary>
    /// Replacement surfacing type.
    /// </summary>
    public string NextSurface { get; set; }

    /// <summary>
    /// Surfacing date as a text/string value in dd/mm/yyyy format.
    /// </summary>
    public string SurfacingDateString { get; set; }

    /// <summary>
    /// Surfacing date in fractional years, calculated from the SurfacingDateString during Initialisation.
    /// </summary>
    public double SurfaceAge { get; set; }

    /// <summary>
    /// Surface function.
    /// </summary>
    public string SurfaceFunction { get; set; }

    /// <summary>
    /// Surfacing material.
    /// </summary>
    public string SurfaceMaterial { get; set; }

    /// <summary>
    /// Surfacing expected life (years) from RAMM.
    /// </summary>
    public double SurfaceExpectedLife { get; set; }

    /// <summary>
    /// Surfacing number of layers
    /// </summary>
    public double SurfaceNumberOfLayers { get; set; }

    /// <summary>
    /// Surfacing thickness in millimetres.
    /// </summary>
    public double SurfaceThickness { get; set; }

    /// <summary>
    /// Pavement type (not in use).
    /// </summary>
    public string PavementType { get; set; }

    /// <summary>
    /// Pavement construction date as a text/string value in dd/mm/yyyy format.
    /// </summary>
    public string PavementDateString { get; set; }

    /// <summary>
    /// Pavement Age in fractional years, calculated from the PavementDateString during Initialisation.
    /// </summary>
    public double PavementAge { get; set; }

    /// <summary>
    /// Age-based pavement remaining life.
    /// </summary>
    public double PavementRemainingLife { get; set; }

    /// <summary>
    /// Returns the percentage of the Expected Pavement Life based on the Pavement Age and Remaining Life.
    /// </summary>
    public double PavementAchievedLife
    {
        get
        {
            double expectedLife = this.PavementAge + this.PavementRemainingLife;
            if (expectedLife <= 0.0)
            {
                throw new Exception($"Pavement expected life is zero or negative for segment {this.FeebackCode}. Pavement Age: {this.PavementAge}, Remaining Life: {this.PavementRemainingLife}.");
            }
            return this.PavementAge / expectedLife * 100.0;
        }
    }

    /// <summary>
    ///  Pavement Risk factor based on traffic loading and pavement life achieved
    /// </summary>
    public double HCVRisk
    {
        get
        {
            // Pavement Risk factor traffic loading component
            double hcv_risk_a = Math.Pow(this.HeavyVehiclesPerDay, 0.1);

            // Pavement Risk factor pavement life achieved component
            double hcv_risk_b = Math.Pow(this.PavementAchievedLife, 0.5);

            // Combine the two components by multiplying them
            double hcv_risk = hcv_risk_a * hcv_risk_b;

            return hcv_risk;
        }
    }


    #endregion

    #region ONRC and Carriageway Attributes

    /// <summary>
    /// Urban/Rural flag.
    /// </summary>
    public string UrbanRural { get; set; }

    /// <summary>
    /// ONRC Category.
    /// </summary>
    public string ONRC { get; set; }

    /// <summary>
    /// NZTA Hierarchy (not in use).
    /// </summary>
    public string NztaHierarchy { get; set; }

    /// <summary>
    /// ONF Street Category (not in use).
    /// </summary>
    public string OnfStreetCategory { get; set; }

    /// <summary>
    /// ONF Movement Rank (not in use).
    /// </summary>
    public string OnfMovementRank { get; set; }

    /// <summary>
    /// ONF Freight (not in use).
    /// </summary>
    public string OnfFreight { get; set; }

    /// <summary>
    /// Road use descriptor (not in use).
    /// </summary>
    public string RoadUse { get; set; }

    /// <summary>
    /// Road class based on ONRC value mapped to a Road Class in lookup set 'road_class'. Note: this does NOT
    /// map to the input column "file_road_class" as that column contains client-variant values.
    /// </summary>
    public string RoadClass { get; set; }

    /// <summary>
    /// Number of lanes (not in use).
    /// </summary>
    public double NumberOfLanes { get; set; }

    /// <summary>
    /// Pavement use code (not in use).
    /// </summary>
    public double PavementUse { get; set; }

    /// <summary>
    /// Combined road type based on Urban/Rural and Road Class. This is simply a concatenation of the two
    /// values, so it will return e.g. 'RL' for Rural, Low Volume, or 'UL' for Urban, Low Volume.
    /// </summary>
    public string RoadType
    {
        get
        {
            return this.UrbanRural + this.RoadClass;
        }
    }

    #endregion

    #region Traffic and Growth

    /// <summary>
    /// Average daily traffic.
    /// </summary>
    public double AverageDailyTraffic { get; set; }

    /// <summary>
    /// Heavy vehicle percentage.
    /// </summary>
    public double HeavyVehiclePercentage { get; set; }

    /// <summary>
    /// Number of bus routes (not in use).
    /// </summary>
    public double NumberOfBusRoutes { get; set; }

    /// <summary>
    /// Traffic growth percentage.
    /// </summary>
    public double TrafficGrowthPercent { get; set; }

    /// <summary>
    /// Heavy vehicles per day, calculated as a percentage of Average Daily Traffic using HeavyVehiclePercentage.
    /// </summary>
    public double HeavyVehiclesPerDay
    {
        get
        {
            return this.AverageDailyTraffic * (this.HeavyVehiclePercentage / 100.0);
        }
    }

    #endregion

    #region Faults and Maintenance

    /// <summary>
    /// Surfacing faults area in square metres.
    /// </summary>
    public double SurfacingFaultsM2 { get; set; }

    /// <summary>
    /// Pavement faults area in square metres.
    /// </summary>
    public double PavementFaultsM2 { get; set; }

    #endregion

    #region High Speed Data (HSD) (Rut, Roughness, Texture etc.)

    /// <summary>
    /// Roughness segment survey date in dd/mm/yyyy format.
    /// </summary>
    public string RoughnessSurveyDate { get; set; }

    /// <summary>
    /// NAASRA 85th percentile roughness.
    /// </summary>
    public double Naasra85 { get; set; }

    /// <summary>
    /// HSD survey date as a string in dd/mm/yyyy format. Do not use this
    /// after initialitation - use the RutParameterValue property instead.
    /// </summary>
    public string HsdSurveyDateString { get; set; }

    /// <summary>
    /// LWP mean rut 85th percentile from raw input values. Do not use this 
    /// after initialisation - use the RutParameterValue property instead.
    /// </summary>
    public double RutLwpMean85 { get; set; }

    /// <summary>
    /// RWP mean rut 85th percentile from raw input values.
    /// </summary>
    public double RutRwpMean85 { get; set; }

    /// <summary>
    /// Rut parameter value calculated during initialisation and used to represent the rutting condition of the road segment.     
    /// </summary>
    public double RutParameterValue { get; set; }

    #endregion

    #region Visual Condition Survey

    /// <summary>
    /// Condition survey date in dd/mm/yyyy format.
    /// </summary>
    public string ConditionSurveyDate { get; set; }

    /// <summary>
    /// Percentage of alligator or mesh cracks.
    /// </summary>
    public double PctAlligatorCracks { get; set; }

    /// <summary>
    /// Percentage of longitudinal and transverse cracks.
    /// </summary>
    public double PctLongTransCracks { get; set; }

    /// <summary>
    /// Percentage of potholes.
    /// </summary>
    public double PctPotholes { get; set; }

    /// <summary>
    /// Percentage of scabbing.
    /// </summary>
    public double PctScabbing { get; set; }

    /// <summary>
    /// Percentage of flushing.
    /// </summary>
    public double PctFlushing { get; set; }

    /// <summary>
    /// Percentage of shoving.
    /// </summary>
    public double PctShoving { get; set; }

    /// <summary>
    /// Percentage of edge breaks.
    /// </summary>
    public double PctEdgeBreaks { get; set; }

    #endregion

}

