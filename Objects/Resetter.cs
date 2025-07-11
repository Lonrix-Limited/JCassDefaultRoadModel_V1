using JCass_ModelCore.Models;
using JCass_ModelCore.Treatments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.Objects;

/// <summary>
/// Class to handle Resetting, with supporting logic and helper functions
/// </summary>
public class Resetter
{

    private ModelBase _frameworkModel;
    private RoadNetworkModel _domainModel;

    public Resetter(ModelBase frameworkModel, RoadNetworkModel domainModel)
    {
        _frameworkModel = frameworkModel ?? throw new ArgumentNullException(nameof(frameworkModel), "Domain model cannot be null");
        _domainModel = domainModel ?? throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
    }

    public RoadSegment Reset(RoadSegment segment, int period, TreatmentInstance treatment)
    {
        if (treatment is null) return segment;

        string treatmentCategory = _frameworkModel.TreatmentTypes[treatment.TreatmentName].Category;
        bool isRehab = treatment.TreatmentName.ToLower().StartsWith("rehab");
        bool isPreseal = treatment.TreatmentName.ToLower().StartsWith("preseal");

        segment.AverageDailyTraffic = segment.AverageDailyTraffic * (1 + segment.TrafficGrowthPercent / 100);
        // No need to reset HCV count as it is automatically calculated based on the AverageDailyTraffic and HCVPercent

        if (isRehab)
        {
            segment.PavementAge = 0;
            segment.PavementRemainingLife = _frameworkModel.GetLookupValueNumber("pavement_expected_life", segment.RoadType);
        }
        else
        {
            segment.PavementAge = segment.PavementAge + 1;
            segment.PavementRemainingLife = segment.PavementRemainingLife - 1;
        }

        // No need to update HCV Risk because it is automatically calculated based on the HCV and Pavement Life Achieved

        segment.SurfaceClass = _frameworkModel.GetLookupValueText("treat_surf_class", treatment.TreatmentName);
        segment.SurfaceMaterial = _frameworkModel.GetLookupValueText("treat_surf_materials", treatment.TreatmentName);
        segment.SurfaceFunction = this.GetSurfaceFunction(treatment.TreatmentName, isPreseal, isRehab, segment.SurfaceFunction);

        // If rehab, number of surfacing becomes 1. Otherwise, increase number of surfacings but only if it is a chipseal. If AC, then it remains the same.
        segment.SurfaceNumberOfLayers = isRehab ? 1 : segment.SurfaceIsChipSealFlag == 1 ? segment.SurfaceNumberOfLayers + 1 : segment.SurfaceNumberOfLayers;
        
        if (isRehab)
        {
            //Surface Thickness to reset to, based on lookup of surface material type applied if treatment is pavement renewal (Rehab)
            segment.SurfaceThickness = _frameworkModel.GetLookupValueNumber("surf_thickness_new", segment.SurfaceMaterial);
        }
        else
        {
            //Surface Thickness to add, based on lookup of surface material type applied if treatment is surface renewal
            segment.SurfaceThickness = segment.SurfaceThickness + _frameworkModel.GetLookupValueNumber("surf_thickness_add", segment.SurfaceMaterial);
        }
        
        segment.SurfaceAge = isPreseal ? segment.SurfaceAge + 1 : 0;  //All treatments reset surface age to zero except if Preseal
        segment.SurfaceExpectedLife = this.GetExpectedSurfaceLife(segment);

        // Reset visual distresses
        segment.PctFlushing = _domainModel.FlushingModel.GetValueAfterReset(segment, segment.PctFlushing,treatmentCategory);
        segment.FlushingModelInfo = _domainModel.FlushingModel.GetResettedSetupValues(segment, segment.PctFlushing, treatmentCategory);

        segment.PctEdgeBreaks = _domainModel.EdgeBreakModel.GetValueAfterReset(segment, segment.PctEdgeBreaks, treatmentCategory);
        segment.EdgeBreakModelInfo = _domainModel.EdgeBreakModel.GetResettedSetupValues(segment, segment.PctEdgeBreaks, treatmentCategory);

        segment.PctScabbing = _domainModel.ScabbingModel.GetValueAfterReset(segment, segment.PctScabbing, treatmentCategory);
        segment.ScabbingModelInfo = _domainModel.ScabbingModel.GetResettedSetupValues(segment, segment.PctScabbing, treatmentCategory);

        segment.PctLongTransCracks = _domainModel.LTCracksModel.GetValueAfterReset(segment, segment.PctLongTransCracks, treatmentCategory);
        segment.LTCracksModelInfo = _domainModel.LTCracksModel.GetResettedSetupValues(segment, segment.PctLongTransCracks, treatmentCategory);

        segment.PctMeshCracks = _domainModel.MeshCrackModel.GetValueAfterReset(segment, segment.PctMeshCracks, treatmentCategory);
        segment.MeshCrackModelInfo = _domainModel.MeshCrackModel.GetResettedSetupValues(segment, segment.PctMeshCracks, treatmentCategory);

        segment.PctShoving = _domainModel.ShovingModel.GetValueAfterReset(segment, segment.PctShoving, treatmentCategory);
        segment.ShovingModelInfo = _domainModel.ShovingModel.GetResettedSetupValues(segment, segment.PctShoving, treatmentCategory);

        segment.PctPotholes = _domainModel.PotholeModel.GetValueAfterReset(segment, segment.PctPotholes, treatmentCategory);
        segment.PotholeModelInfo = _domainModel.PotholeModel.GetResettedSetupValues(segment, segment.PctPotholes, treatmentCategory);


        segment.RutParameterValue = this.GetResetttedRut(segment, isRehab);
        segment.Naasra85 = this.GetResetttedNaasra(segment, isRehab);

        // Increase the treatment count for the segment. This will also mark the treatment as treated, and reset the 
        // historical maintenance quantities so that it no longer influences PDI and SDI calculations.
        segment.TreatmentCount++;

        return segment;

    }

    private string GetSurfaceFunction(string treatmentName, bool isPreseal, bool isRehab, string currentSurfaceFunction)
    {
        if (isPreseal) return "1a";
        if (isRehab) return "1";

        if (treatmentName.ToLower().StartsWith("rehab_ac")) return "2";

        if (currentSurfaceFunction == "1a") return "H";

        if (currentSurfaceFunction == "1") return "2";

        if (currentSurfaceFunction == "2") return "R";

        // If none of the others are fired, return the current surface function, e.g surface is aready a "R"
        return currentSurfaceFunction;


    }

    private double GetExpectedSurfaceLife(RoadSegment segment)
    {
        if (segment.SurfaceClass == "blocks") return segment.SurfaceExpectedLife; // Blocks have a fixed expected life, no lookup needed
        if (segment.SurfaceClass == "concrete") return segment.SurfaceExpectedLife; // Concrete has a fixed expected life, no lookup needed
        if (segment.SurfaceClass == "other") return segment.SurfaceExpectedLife; 

        string lookupKey = $"{segment.SurfaceFunction}_{segment.SurfaceMaterial}_{segment.RoadClass}";
        bool keyExists = _frameworkModel.Lookups["surf_expected_life"].ContainsKey(lookupKey);
        if (keyExists)
        {
            return _frameworkModel.GetLookupValueNumber("surf_expected_life", lookupKey);
        }
        else
        {
            _frameworkModel.LogMessage($"Expected surface life lookup key '{lookupKey}' not found. Using default value for {segment.SurfaceClass}.", true);
            if (segment.SurfaceClass == "cs") // Chipseal
            {
                return _frameworkModel.GetLookupValueNumber("surf_expected_life", "cs_undefined");
            }
            else if (segment.SurfaceClass == "ac") // Asphalt Concrete
            {
                return _frameworkModel.GetLookupValueNumber("surf_expected_life", "ac_undefined");
            }            
        }
        throw new KeyNotFoundException($"Expected surface life not found for {segment.FeebackCode}. Surface class = '{segment.SurfaceClass}'.");
    }
    
    private double GetResetttedRut(RoadSegment segment, bool isRehab)
    {
        if (segment.SurfaceIsChipSealOrACFlag == 0)
        {
            return segment.RutParameterValue; // Rut remains constant for non ChipSeal or AC surfaces
        }

        if (segment.SurfaceFunction == "1a")
        {
            return segment.RutParameterValue; // This indicates preseal has just been applied, so no reset
        }

        if (isRehab)
        {
            return _frameworkModel.GetLookupValueNumber("rehab_resets_rut", segment.SurfaceRoadType);
        }
        else
        {
            double exceedanceThreshold = _frameworkModel.GetLookupValueNumber("reset_exceed_thresh_rut", segment.SurfaceRoadType);
            double improvementFactor = _frameworkModel.GetLookupValueNumber("reset_perc_improv_facts_rut", segment.SurfaceRoadType);

            double exceedance = segment.RutParameterValue <= exceedanceThreshold ? 0 : exceedanceThreshold - segment.RutParameterValue;
            return segment.RutParameterValue + (exceedance * improvementFactor);
        }
            

    }

    private double GetResetttedNaasra(RoadSegment segment, bool isRehab)
    {
        if (segment.SurfaceIsChipSealOrACFlag == 0)
        {
            return segment.RutParameterValue; // Rut remains constant for non ChipSeal or AC surfaces
        }

        if (segment.SurfaceFunction == "1a")
        {
            return segment.RutParameterValue; // This indicates preseal has just been applied, so no reset
        }

        if (isRehab)
        {
            return _frameworkModel.GetLookupValueNumber("rehab_resets_naasra", segment.SurfaceRoadType);
        }
        else
        {
            double exceedanceThreshold = _frameworkModel.GetLookupValueNumber("reset_exceed_thresh_naasra", segment.SurfaceRoadType);
            double improvementFactor = _frameworkModel.GetLookupValueNumber("reset_perc_improv_facts_naasra", segment.SurfaceRoadType);

            double exceedance = segment.Naasra85 <= exceedanceThreshold ? 0 : exceedanceThreshold - segment.Naasra85;
            return segment.Naasra85 + (exceedance * improvementFactor);
        }


    }


}
