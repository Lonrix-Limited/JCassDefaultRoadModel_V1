using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.EMMA;
using JCass_Economics.Utilities;
using JCass_Functions.Engineering;
using JCass_Functions.Lookups;
using JCass_ModelCore.DomainModels;
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

        int periodsToNextTreatment = Convert.ToInt16(infoFromModel[" periods_to_next_treatment"]);

        // Check if the segment passes the Candidate Selection checks. If not, return an empty list.
        var csResult = CandidateSelector.EvaluateCandidate(segment, _frameworkModel, _domainModel, period, periodsToNextTreatment);
        if (csResult.IsValidCandidate == false) return triggeredTreatments;

        // Check if second coat after Rehabilitation should be added. If so, since we are forcing it, do not look
        // for other candidate treatments
        this.AddSecondCoatIfValid(segment, period, triggeredTreatments);
        if (triggeredTreatments.Count > 0) return triggeredTreatments;

        // Check if a second coat after Preseal Repairs should be added, If so, since we are forcing it, do not look
        // for other candidate treatments
        this.AddHoldingFollowUpSurfacingIfValid(segment, period, triggeredTreatments);
        if (triggeredTreatments.Count > 0) return triggeredTreatments;

        // Check if a birthday treatment should be added. If so, since we are forcing it, do not look for other candidate treatments
        this.AddBirthdayTreatmentBlocksOrConcreteIfValid(segment, period, triggeredTreatments);
        if (triggeredTreatments.Count > 0) return triggeredTreatments;

        // If we get here, we know that no second coats or birthday treatments are added. Now find candidate treatments
        // to add to the optimisation stage
        this.AddPreservationTreatmentIfValid(segment, period, triggeredTreatments);
        
        this.AddPresealOnChipsealfValid(segment, period, triggeredTreatments);
        this.AddPresealOnAsphaltIfValid(segment, period, triggeredTreatments);
        this.AddRehabilitationIfValid(segment, period, triggeredTreatments);


        return triggeredTreatments;
    }

    #region Preliminary checks for treatments

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

    private bool CanDoAsphaltPreservation(RoadSegment segment)
    {
        //n : renw_secondcoat_flag = 0 AND n : para_csl_flag = 1 AND n : pcal_next_surf_cs_flag = 1 AND n : periods_to_next_treatment > 6

        //Note: Check for 'periods_to_next_treatment > 6' is done in CandidateSelector.EvaluateCandidate method, so we do not need to check it here again.

        if (segment.AsphaltOkFlag == 0) return false; // If the segment is not suitable for asphalt preservation, do not add a treatment

        // If next surface is intended to be ChipSeal, not valid
        if (segment.NextSurfaceIsChipSeal == true) return false;

        // Do not add a treatment if the current surface is a ChipSeal
        // ToDo: needs discussion. May be cases where current surfacing is AC
        if (segment.SurfaceIsChipSealFlag == 1) return false;

        if (segment.SecondCoatNeeded) return false; // Do not add a preservation treatment if a second coat is needed

        return true;

    }

    private bool CanDoRehabilitationOnChipSeal(RoadSegment segment)
    {
        //n : para_csl_flag = 1 AND n : pcal_can_rehab_flag = 1 AND n : pcal_next_surf_cs_flag = 1 AND n : periods_to_next_treatment > 6

        //Note: Check for 'periods_to_next_treatment > 6' is done in CandidateSelector.EvaluateCandidate method, so we do not need to check it here again.
        
        if (segment.CanRehabFlag == 0) return false; // If the segment cannot be rehabilitated, do not add a treatment

        // If next surface is not intended to be ChipSeal, do not add a treatment
        if (segment.NextSurfaceIsChipSeal == false) return false;

        // Do not add a treatment if the current surface is not ChipSeal
        // ToDo: needs discussion. May be cases where current surfacing is AC
        if (segment.SurfaceIsChipSealFlag == 0) return false;
        
        return true;

    }

    private bool CanDoRehabilitationOnAsphalt(RoadSegment segment)
    {
        //n : para_csl_flag = 1 AND n : pcal_can_rehab_flag = 1 AND n : pcal_next_surf_ac_flag = 1 AND n : periods_to_next_treatment > 6

        //Note: Check for 'periods_to_next_treatment > 6' is done in CandidateSelector.EvaluateCandidate method, so we do not need to check it here again.

        if (segment.CanRehabFlag == 0) return false; // If the segment cannot be rehabilitated, do not add a treatment

        // If next surface is not intended to be AC, do not consider
        if (segment.NextSurface != "ac") return false;

        // Only valid if surface is asphalt
        // ToDo: needs discussion. May be cases where current surfacing is AC
        if (segment.SurfaceIsChipSealFlag == 1) return false;

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

    private bool CanDoPresealOnAsphalt(RoadSegment segment, int iPeriod, out double presealAreaFraction)
    {
        //n : para_csl_flag = 1 AND n : pcal_next_surf_ac_flag = 1 AND n : renw_preseal_area_fraction > 0 AND t : para_surf_func != 1a AND n : periods_to_next_treatment > 6

        //Note: Check for 'periods_to_next_treatment > 6' is done in CandidateSelector.EvaluateCandidate method, so we do not need to check it here again.

        presealAreaFraction = 0.0; // Default value

        if (CanDoAsphaltPreservation(segment) == false) return false;

        if (segment.RutParameterValue > _domainModel.Constants.TSSPreserveMaxRut) return false; // If the rut depth is above the maximum threshold, do not add a treatment

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

    #endregion

    private void AddBirthdayTreatmentBlocksOrConcreteIfValid(RoadSegment segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        //n : pcal_can_treat_flag = 1 AND n : pcal_next_surf_blocks_flag = 1 AND n : period >= file_earliest_treat_period AND n : para_surf_remain_life <= 1

        if (segment.CanTreatFlag == 0) return; // If the segment cannot be treated, do not add a treatment
        string treatmentName = "";

        switch (segment.NextSurface)
        {
            case "blocks":
                treatmentName = "BlockRep";
                break;
            case "concrete":
                treatmentName = "ConcRep";
                break;
            case "other":
                treatmentName = "Xtreat";
                break;
            default:
                //If we get here, it is ChipSeal or Asphalt, which are not valid for this treatment
                return;
        }

        if (segment.SurfaceRemainingLife > 1) return; // If the surface remaining life is greater than 1, do not add a treatment
        if (iPeriod < segment.EarliestTreatmentPeriod) return; // If the period is less than the earliest treatment period, do not add a treatment

        //If we get here, a birthday treatment is valid
        double quantity = segment.AreaSquareMetre;
        bool forceTreatment = true;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, forceTreatment,  "Birthday treatment", "");
        treatment.TreatmentSuitabilityScore = 102; // Set a high suitability score for second coat treatments
        treatments.Add(treatment);

    }

    private void AddHoldingFollowUpSurfacingIfValid(RoadSegment segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        string treatmentName = "";
        string reason = "";
        string comment = "";
        if (segment.SurfaceFunction != "1a") return;
        
        if (segment.NextSurfaceIsChipSeal == true)
        {
            if (this.CanDoChipsealPreservation(segment) == false) return;
            treatmentName = "ChipSeal_H";
            reason = "Pre-seal follow-up";           
        }
        else
        {
            if (this.CanDoAsphaltPreservation(segment) == false) return;
            treatmentName = "ThinAC_H";
            reason = "Pre-seal follow-up";
        }               
        double quantity = segment.AreaSquareMetre;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, true, reason, comment);
        treatment.TreatmentSuitabilityScore = 102;  //fixed high score to force this treatment to be selected if it is valid
        treatments.Add(treatment);
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

    private void AddPreservationTreatmentIfValid(RoadSegment segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        string treatmentName = "";
        if (segment.NextSurfaceIsChipSeal == true)
        {
            if (this.CanDoChipsealPreservation(segment) == false) return;
            treatmentName = "ChipSeal_P";
        }
        else
        {
            if (this.CanDoAsphaltPreservation(segment) == false) return;
            treatmentName = "ThinAC_P";
        }

        // If the rut depth is above the maximum threshold, do not add a treatment
        if (segment.RutParameterValue > _domainModel.Constants.TSSPreserveMaxRut) return;

        // If the surface life achieved is not greater than the minimum required, do not add a treatment
        if (segment.SurfaceAchievedLifePercent < _domainModel.Constants.TSSPreserveMinSla) return;

        // For preservation, if PDI is above the maximum threshold, do not add a treatment
        if (segment.GetPavementDistressIndex(_frameworkModel, _domainModel, iPeriod) > _domainModel.Constants.TSSPreserveMaxPdi) return;

        double tssScore = TreatmentSuitabilityScorer.GetTSSForPreservationTreatment(segment, _frameworkModel, _domainModel, iPeriod);

        double sdi = segment.GetSurfaceDistressIndex(_frameworkModel, _domainModel, iPeriod);        
        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"SDI={Math.Round(sdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double quantity = segment.AreaSquareMetre;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity,false, reason, comment);
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

        TreatmentInstance treatment = this.GetPresealTreatment(segment, iPeriod, "Preseal_CS");
        treatments.Add(treatment);

    }

    private void AddPresealOnAsphaltIfValid(RoadSegment segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        if (this.CanDoPresealOnAsphalt(segment, iPeriod, out double presealAreaFraction) == false) return;

        double presealArea = segment.AreaSquareMetre * presealAreaFraction;

        // If the rut depth is above the maximum threshold, do not add a treatment
        if (segment.RutParameterValue > _domainModel.Constants.TSSPreserveMaxRut) return;

        // If the surface life achieved is not greater than the minimum required, do not add a treatment
        if (segment.SurfaceAchievedLifePercent < _domainModel.Constants.TSSPreserveMinSla) return;

        // For preservation, if PDI is above the maximum threshold, do not add a treatment
        if (segment.GetPavementDistressIndex(_frameworkModel, _domainModel, iPeriod) > _domainModel.Constants.TSSPreserveMaxPdi) return;

        TreatmentInstance treatment = this.GetPresealTreatment(segment, iPeriod, "Preseal_AC");
        treatments.Add(treatment);

    }

    private void AddRehabilitationIfValid(RoadSegment segment, int iPeriod, List<TreatmentInstance> treatments)
    {
        string treatmentName = "";
        if (segment.NextSurfaceIsChipSeal == true)
        {
            if (this.CanDoRehabilitationOnChipSeal(segment) == false) return;
            treatmentName = "Rehab_CS_" + segment.RoadClass.ToUpper();
        }
        else
        {
            if (this.CanDoRehabilitationOnAsphalt(segment) == false) return;
            treatmentName = "Rehab_AC_" + segment.RoadClass.ToUpper();
        }

        double pdi = segment.GetPavementDistressIndex(_frameworkModel, _domainModel, iPeriod);
        
        double tssScore = TreatmentSuitabilityScorer.GetTSSForRehabilitation(segment, _frameworkModel, _domainModel, iPeriod);
        
        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"PDI={Math.Round(pdi, 1)}, TSS={Math.Round(tssScore, 2)}";



        double quantity = segment.AreaSquareMetre;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, false, reason, comment);
        treatment.TreatmentSuitabilityScore = tssScore;
        treatments.Add(treatment);
    }

    private TreatmentInstance GetPresealTreatment(RoadSegment segment, int iPeriod, string treatmentName)
    {
        double tssScore = 0;
        if (segment.CanRehabFlag == 1)
        {
            // If this is a rehab route, then Preseal must compete with Rehab. Thus calculate the TSS for Preseal since
            // Rehab will be competing based on its TSS score.
            tssScore = TreatmentSuitabilityScorer.GetTSSForPresealRepairs(segment, _frameworkModel, _domainModel, iPeriod);            
        }
        else
        {
            // If this is NOT a rehab route, then Preseal is considered as a Rehabilitation. Thus the TSS in this case
            // should be based on the TSS for Rehabilitation.
            tssScore = TreatmentSuitabilityScorer.GetTSSForRehabilitation(segment, _frameworkModel, _domainModel, iPeriod);
        }


        double pdi = segment.GetPavementDistressIndex(_frameworkModel, _domainModel, iPeriod);

        string reason = $"SLA={Math.Round(segment.SurfaceAchievedLifePercent, 1)}";
        string comment = $"PDI={Math.Round(pdi, 1)}, TSS={Math.Round(tssScore, 2)}";

        double quantity = segment.AreaSquareMetre;
        TreatmentInstance treatment = new TreatmentInstance(segment.ElementIndex, treatmentName, iPeriod, quantity, false, reason, comment);
        treatment.TreatmentSuitabilityScore = tssScore;
        return treatment;
    }
       
}
