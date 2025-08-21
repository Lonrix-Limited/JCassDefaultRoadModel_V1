using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using JCass_Functions.Engineering;
using JCass_ModelCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;

namespace JCassDefaultRoadModel.Objects;

public record CandidateSelectionResult(bool IsValidCandidate, string reason)
{
    public string Outcome
    {
        get
        {
            return IsValidCandidate ? "ok" : $"{reason}";
        }
    }
}


public static class CandidateSelector
{

    public static CandidateSelectionResult EvaluateCandidate(RoadSegment segment, ModelBase frameworkModel, RoadNetworkModel domainModel, int currentPeriod, int periodsToNextTreatment)
    {
		try
		{
            // First check for all prelimiary conditions that, if met, will cancel or allow the candidate to proceed (e.g. second-coat needed,
            // earliest treatment period, minimum surface age, etc.)
            CandidateSelectionResult prelimCheckResult = EvaluatePreliminaries(segment, frameworkModel, domainModel, currentPeriod, periodsToNextTreatment);
            if (prelimCheckResult != null)
            {
                return prelimCheckResult; // If any preliminary checks fail, return the result
            }

            // If we reach here, it means the segment has passed all preliminary checks, so we can proceed with further evaluation
            // based on conditions and segment length.
            bool isShortSegment = segment.LengthInMetre < domainModel.Constants.CSShortSegLength;
            bool isShortTerm = currentPeriod <= domainModel.Constants.CSShortTermPeriod;

            if (isShortTerm)
            {
                // Currently, check on ADT is only applied in short term, so that over the long term, even very short segments can be considered for treatment
                // otherwise they lag behing and mess up network condition statistics
                if (segment.AverageDailyTraffic < domainModel.Constants.CSMinAdtThreshold)
                {
                    return new CandidateSelectionResult(false, $"ADT = {Math.Round(segment.AverageDailyTraffic, 1)}: below threshold ({domainModel.Constants.CSMinAdtThreshold})");
                }

                if (isShortSegment)
                {                    
                    if (HasSufficientDistressForShortSegment(segment, frameworkModel, domainModel, currentPeriod) == false)
                    {
                        return new CandidateSelectionResult(false, $"Short Segment, Short Term: Distress too low");
                    }
                    else
                    {
                        return new CandidateSelectionResult(true, $"Short Segment, Short Term: Distress above threshold");
                    }
                }
                else
                {
                    //Check if the segment is a valid candidate for long segments
                    if (HasSufficientDistressForLongSegment(segment, frameworkModel, domainModel) == false)
                    {
                        return new CandidateSelectionResult(false, $"Long Segment, Short Term: Distress not sufficient for treatment");
                    }
                    else
                    {
                        return new CandidateSelectionResult(true, $"Short Segment, Short Term: Distress above threshold");
                    }
                }
            }
            else
            {
                // Evaluate for long term - does not distinguish between short and long segments
                //Trigger outcome for Long Term trigger logic(see flowchart)
                double pdi = CalculationUtilities.GetPavementDistressIndex(segment, frameworkModel, domainModel, currentPeriod);
                double sdi = CalculationUtilities.GetSurfacingDistressIndex(segment, frameworkModel, domainModel, currentPeriod);

                if (pdi > domainModel.Constants.CSMinPdiToTreat || sdi > domainModel.Constants.CSMinSdiToTreat)
                {
                    return new CandidateSelectionResult(true, $"Long term; PDI = {Math.Round(pdi, 2)} or SDI = {Math.Round(sdi, 2)}: above thresholds ({domainModel.Constants.CSMinPdiToTreat}, {domainModel.Constants.CSMinSdiToTreat})");
                }
                else
                {
                    if (pdi <= domainModel.Constants.CSMinPdiToTreat)
                    {
                        return new CandidateSelectionResult(false, $"Long term; PDI = {Math.Round(pdi, 2)}: below threshold ({domainModel.Constants.CSMinPdiToTreat})");
                    }

                    if (sdi <= domainModel.Constants.CSMinSdiToTreat)
                    {
                        return new CandidateSelectionResult(false, $"Long term; SDI = {Math.Round(sdi, 2)}: below threshold ({domainModel.Constants.CSMinPdiToTreat}");
                    }

                    throw new Exception($"CSA decision logic does not evaluate for {segment.FeebackCode} in period {currentPeriod}");
                }                
            }
        }
        catch (Exception ex)
		{
			throw new Exception($"Error checking if {segment.FeebackCode} is a valid Candidate for treatment. Details: {ex.Message}");
		}
    }

    /// <summary>
    /// Does preliminary checks on the segment to determine if it is a valid candidate for treatment. For example, checks if a second coat is 
    /// needed, or if the minimum surface age or SLA has been reached, or if the specified earliest treatment period has been reached.
    /// If any of these checks are definitive, the outcome is returned. If none of these checks are definitive, the method returns null, 
    /// indicating that further checks are needed to determine if the segment is a valid candidate for treatment.
    /// </summary>    
    private static CandidateSelectionResult EvaluatePreliminaries(RoadSegment segment, ModelBase frameworkModel, RoadNetworkModel domainModel, 
                                                                   int currentPeriod, int periodsToNextTreatment)
    {        
        if (periodsToNextTreatment <= 6) // If the next treatment is within 6 periods, we do not consider this segment for treatment
        {
            return new CandidateSelectionResult(false, $"Next treatment in {periodsToNextTreatment} periods: too soon");
        }

        //Does this segment require a second coat now? If so, it is a valid candidate, so look no further.
        if (segment.SecondCoatNeeded) { return new CandidateSelectionResult(true, "Second-Coat Needed"); }

        // If surface function is '1a' a follow-up surfacing after Preseal repairs is needed, so open the gate
        if (segment.SurfaceFunction == "1a") { return new CandidateSelectionResult(true, "Second-Coat Needed over Preseal Repairs"); }

        // Is the specified earliest treatment period for the segment reached?
        int adjustedPeriod = currentPeriod + 1;  //Adjusted modelling period to account for a post calc lag in para csl flag
        if (adjustedPeriod < segment.EarliestTreatmentPeriod)
        {
            return new CandidateSelectionResult(false, $"Earliest treatment period {segment.EarliestTreatmentPeriod} not reached");
        }

        // Is the specified minimum surface age reached?
        if (segment.SurfaceAge < domainModel.Constants.CSMinSurfAge)
        {
            return new CandidateSelectionResult(false, $"Surface Age = {Math.Round(segment.SurfaceAge,2)}: below threshold ({domainModel.Constants.CSMinSurfAge})");
        }

        // Is the specified minimum Surface Life Achieved (SLA) reached?
        if (segment.SurfaceClass == "ac" && segment.SurfaceAchievedLifePercent < domainModel.Constants.CSMinSlaToTreatAc)
        {
            return new CandidateSelectionResult(false, $"SLA = {Math.Round(segment.SurfaceAchievedLifePercent, 2)}: below threshold ({domainModel.Constants.CSMinSlaToTreatAc})");
        }

        if (segment.SurfaceClass == "cs" && segment.SurfaceAchievedLifePercent < domainModel.Constants.CSMinSlaToTreatCs)
        {
            return new CandidateSelectionResult(false, $"SLA = {Math.Round(segment.SurfaceAchievedLifePercent,2)}: below threshold ({domainModel.Constants.CSMinSlaToTreatCs})");
        }
        
        return null; // No preliminary issues found, so we can proceed with further checks

    }

    private static bool HasSufficientDistressForShortSegment(RoadSegment segment, ModelBase frameworkModel, RoadNetworkModel domainModel, int currentPeriod)
    {
        double flushLength = (segment.PctFlushing / 100) * segment.LengthInMetre;
        double scabLength = (segment.PctScabbing / 100) * segment.LengthInMetre;
        double meshCrackLength = (segment.PctMeshCracks / 100) * segment.LengthInMetre;
        double shoveLength = (segment.PctShoving / 100) * segment.LengthInMetre;


        int count1Over = 0;
        if (flushLength > domainModel.Constants.CSShortSegDistress1Limit) count1Over++;
        if (scabLength > domainModel.Constants.CSShortSegDistress1Limit) count1Over++;
        if (meshCrackLength > domainModel.Constants.CSShortSegDistress1Limit) count1Over++;
        if (shoveLength > domainModel.Constants.CSShortSegDistress1Limit) count1Over++;


        //Count the times each distress is over a specified length (the specified length comes from lookup 'short_seg_distress2_limit')
        int count2Over = 0;
        if (flushLength > domainModel.Constants.CSShortSegDistress2Limit) count2Over++;
        if (scabLength > domainModel.Constants.CSShortSegDistress2Limit) count2Over++;
        if (meshCrackLength > domainModel.Constants.CSShortSegDistress2Limit) count2Over++;
        if (shoveLength > domainModel.Constants.CSShortSegDistress2Limit) count2Over++;

        if (count1Over > 0 || count2Over > 1)
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    private static bool HasSufficientDistressForLongSegment(RoadSegment segment, ModelBase frameworkModel, RoadNetworkModel domainModel)
    {
        double boostedPotholes = segment.PctPotholes * domainModel.Constants.PotholeBoostFactor;
        double distressPercent = segment.PctFlushing + segment.PctScabbing + segment.PctMeshCracks + segment.PctShoving
            + boostedPotholes + segment.FaultsAndMaintenancePavementPercent;

        // Get the threshold for distress based on Surface Life Achieved. The lower SLA, the higher the distress threshold
        // because we need more distress to motivate for treating a younger segment.
        string slaModelSetupCode = "30,70 | 50,30 | 80,5 | 100,0 | 150,0";
        PieceWiseLinearModelGeneric slaThresholdModel = new PieceWiseLinearModelGeneric(slaModelSetupCode, false);
        double slaThreshold = slaThresholdModel.GetValue(segment.SurfaceAchievedLifePercent);

        // For longer sections only - detect if the sum of key distresses is greater than the distress percentage identified in the piecewise linear function above
        if (distressPercent > slaThreshold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }



}
