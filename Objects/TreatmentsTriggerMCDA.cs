using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.EMMA;
using JCass_Economics.Utilities;
using JCass_Functions.Engineering;
using JCass_Functions.Lookups;
using JCass_ModelCore.Models;
using JCass_ModelCore.Treatments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.Objects;

/// <summary>
/// Class for checking treatments triggering for a MCDA (Multi-Criteria Decision Analysis) model
/// </summary>
public class TreatmentsTriggerMCDA
{
    private ModelBase _frameworkModel;
    private RoadNetworkModel _domainModel;

    public TreatmentsTriggerMCDA(ModelBase frameworkModel, RoadNetworkModel domainModel)
    {
        _frameworkModel = frameworkModel ?? throw new ArgumentNullException(nameof(frameworkModel), "Domain model cannot be null");
        _domainModel = domainModel ?? throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
    }

    public List<TreatmentInstance> GetTriggeredTreatments(RoadSegment segment, int period, Dictionary<string, object> infoFromModel)
    {
        List<TreatmentInstance> triggeredTreatments = new List<TreatmentInstance>();

        // Check if the segment passes the Candidate Selection checks. If not, return an empty list.
        var csResult = CandidateSelector.EvaluateCandidate(segment, _frameworkModel, _domainModel, period);
        if (csResult.IsValidCandidate == false) return triggeredTreatments;

        this.AddSecondCoatIfValid(segment, period, triggeredTreatments);
        this.AddPreservationChipsealfValid(segment, period, triggeredTreatments);

        return triggeredTreatments;
    }


    private bool TriggerPreservationSeal(RoadSegment segment, int period)
    {
        // Only consider Chipseal on segment that are chipseals and for which a SecondCoat is not needed
        if (segment.SurfaceIsChipSealFlag == 1 && segment.SecondCoatNeeded == false)
        {
        }
        return false;
    }


    private void AddSecondCoatIfValid(RoadSegment segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        // Only add a second coat if it is needed and the segment is a valid candidate
        if (segment.SecondCoatNeeded)
        {
            double quantity = segment.AreaSquareMetre;
            TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, "SecondCoat", iPeriod, quantity, true, "Second coat", "Second coat");
            treatment.TreatmentSuitabilityScore = 102; // Set a high suitability score for second coat treatments
            treatments.Add(treatment);
        }        
    }

    private bool CanDoChipsealPreservation(RoadSegment segment)
    {
        //n : renw_secondcoat_flag = 0 AND n : para_csl_flag = 1 AND n : pcal_next_surf_cs_flag = 1 AND n : periods_to_next_treatment > 6

        //Note: Check for 'periods_to_next_treatment > 6' is done in CandidateSelector.EvaluateCandidate method, so we do not need to check it here again.

        // If next surface is not intended to be ChipSeal, do not add a treatment
        if (segment.NextSurfaceIsChipSeal == false) return false;

        // Do not add a treatment if the current surface is not ChipSeal
        // ToDo: needs discussion. May be cases where current surfacing is AC
        if (segment.SurfaceIsChipSealFlag == 0) return false;

        if (segment.SecondCoatNeeded) return false; // Do not add a preservation treatment if a second coat is needed

        return true;

    }

    private bool CanDoPresealOnChipSeal(RoadSegment segment, int iPeriod, out double presealAreaFraction)
    {
        //n : renw_preserve_cs_flag = 1 AND n : renw_preseal_area_fraction > 0 AND t : para_surf_func != 1a  AND n : periods_to_next_treatment > 6

        //Note: Check for 'periods_to_next_treatment > 6' is done in CandidateSelector.EvaluateCandidate method, so we do not need to check it here again.

        presealAreaFraction = 0.0; // Default value

        if (CanDoChipsealPreservation(segment) == false) return false;

        if (segment.SurfaceFunction == "1a") return false; // Do not add a preseal treatment if the surface function is "1a"

        JFuncLookupNumber presealAreaFractionLookup = new JFuncLookupNumber("preseal_effective: para_pdi", _frameworkModel.Lookups);
        Dictionary<string, object> paramVals = new Dictionary<string, object>
        {
            { "para_pdi", segment.GetPavementDistressIndex(_frameworkModel, _domainModel, iPeriod) }
        };
        presealAreaFraction = Convert.ToDouble(presealAreaFractionLookup.Evaluate(paramVals));
        if (presealAreaFraction <= 0.0) return false; // If preseal area fraction is zero or negative, do not add a treatment
        
        return true;

    }

    private void AddPreservationChipsealfValid(RoadSegment segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        if (this.CanDoChipsealPreservation(segment) == false) return;

        // If the rut depth is above the maximum threshold, do not add a treatment
        if (segment.RutParameterValue > _domainModel.Constants.TSSPreserveMaxRut) return;

        // If the surface life achieved is not greater than the minimum required, do not add a treatment
        if (segment.SurfaceAchievedLifePercent < _domainModel.Constants.TSSPreserveMinSla) return;

        // For preservation, if PDI is above the maximum threshold, do not add a treatment
        if (segment.GetPavementDistressIndex(_frameworkModel, _domainModel, iPeriod) > _domainModel.Constants.TSSPreserveMaxPdi) return;

        // If we get here, a preservation treatment is valid. Calculate the relative suitability score based on the Surface Distress Index (SDI)
        string tssModelSetup = $"{_domainModel.Constants.TSSPreserveSdiRank},0|100,100";        
        PieceWiseLinearModelGeneric tssModel = new PieceWiseLinearModelGeneric(tssModelSetup, true);

        double sdi = segment.GetSurfaceDistressIndex(_frameworkModel, _domainModel, iPeriod);
        double tssScore = tssModel.GetValue(sdi);
        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent,1)}";
        string comment = $"SDI={Math.Round(sdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double quantity = segment.AreaSquareMetre;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, "ChipSeal_P", iPeriod, quantity, true, reason, comment);
        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }

    private void AddPresealOnChipsealfValid(RoadSegment segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        if (this.CanDoPresealOnChipSeal(segment, iPeriod, out double presealAreaFraction) == false) return;

        double presealArea = segment.AreaSquareMetre * presealAreaFraction;

        // If the rut depth is above the maximum threshold, do not add a treatment
        if (segment.RutParameterValue > _domainModel.Constants.TSSPreserveMaxRut) return;

        // If the surface life achieved is not greater than the minimum required, do not add a treatment
        if (segment.SurfaceAchievedLifePercent < _domainModel.Constants.TSSPreserveMinSla) return;

        // For preservation, if PDI is above the maximum threshold, do not add a treatment
        if (segment.GetPavementDistressIndex(_frameworkModel, _domainModel, iPeriod) > _domainModel.Constants.TSSPreserveMaxPdi) return;

        // If we get here, a preservation treatment is valid. Calculate the relative suitability score based on the Surface Distress Index (SDI)
        string tssModelSetup = $"{_domainModel.Constants.TSSPreserveSdiRank},0|100,100";
        PieceWiseLinearModelGeneric tssModel = new PieceWiseLinearModelGeneric(tssModelSetup, true);

        double sdi = segment.GetSurfaceDistressIndex(_frameworkModel, _domainModel, iPeriod);
        double tssScore = tssModel.GetValue(sdi);
        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"SDI={Math.Round(sdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double quantity = segment.AreaSquareMetre;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, "ChipSeal_P", iPeriod, quantity, true, reason, comment);
        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }


    //private TreatmentInstance GetSecondCoat(RoadSegment segment, int iPeriod)
    //{

    //    double quantity = segment.AreaSquareMetre;

    //    double suitabilityScore = TreatmentInstance.GetSetupFunctionValueNumber(setupRow, functionValues, "suitability_function");
    //    bool forceTreatment = JCass_Core.Utils.HelperMethods.GetBoolean(setupRow["force"].ToString());

    //    //For a treatment to be a viable candidate, it must either be Forced (e.g. second-coat) or else the treatment suitability score must
    //    //be higher than the specified minimum (specified in Meta-Setup file).
    //    if (forceTreatment || suitabilityScore > model.Configuration.MinimumTreatmentSuitabilityScoreAllowed)
    //    {
    //        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, "SecondCoat", iPeriod, quantity, forceTreatment, "Second coat", "Second coat");
    //        treatment.TreatmentSuitabilityScore = suitabilityScore;
    //        treatments.Add(treatment);
    //    }
    //}

}
