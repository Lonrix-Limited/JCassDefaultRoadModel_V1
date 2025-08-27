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
            // Is the specified earliest treatment period for the segment reached?
            // TODO: Needs discussion - should we override second coats with this flag (as is the case now), or not?
            int adjustedPeriod = currentPeriod + 1;  //Adjusted modelling period to account for a post calc lag in para csl flag
            if (adjustedPeriod < segment.EarliestTreatmentPeriod)
            {
                return new CandidateSelectionResult(false, $"Earliest treatment period {segment.EarliestTreatmentPeriod} not reached");
            }

            //Does this segment require a second coat now? If so, it is a valid candidate, so look no further.
            if (segment.SecondCoatNeeded) { return new CandidateSelectionResult(true, "Second-Coat Needed"); }

            // If surface function is '1a' a follow-up surfacing after Preseal repairs is needed, so look no further.
            if (segment.SurfaceFunction == "1a") { return new CandidateSelectionResult(true, "Second-Coat Needed over Preseal Repairs"); }

            // If there is a committed treatment in the near future, then this segment is not a candidate for treatment
            if (periodsToNextTreatment <= domainModel.Constants.CSMinPeriodsToNextTreat) 
            {
                return new CandidateSelectionResult(false, $"Next treatment in {periodsToNextTreatment} periods: too soon");
            }
                        
            // Is the specified minimum surface age reached?
            if (segment.SurfaceAge < domainModel.Constants.CSMinSurfAge)
            {
                return new CandidateSelectionResult(false, $"Surface Age = {Math.Round(segment.SurfaceAge, 2)}: below threshold ({domainModel.Constants.CSMinSurfAge})");
            }

            // Is the specified minimum Surface Life Achieved (SLA) reached?
            if (segment.SurfaceClass == "ac" && segment.SurfaceAchievedLifePercent < domainModel.Constants.CSMinSlaToTreatAc)
            {
                return new CandidateSelectionResult(false, $"SLA = {Math.Round(segment.SurfaceAchievedLifePercent, 2)}: below threshold ({domainModel.Constants.CSMinSlaToTreatAc})");
            }

            if (segment.SurfaceClass == "cs" && segment.SurfaceAchievedLifePercent < domainModel.Constants.CSMinSlaToTreatCs)
            {
                return new CandidateSelectionResult(false, $"SLA = {Math.Round(segment.SurfaceAchievedLifePercent, 2)}: below threshold ({domainModel.Constants.CSMinSlaToTreatCs})");
            }

            // Finally, check if the segment meets the minimum distress indices for treatment
            if (segment.SurfaceDistressIndex < domainModel.Constants.CSMinSDIToTreat && segment.PavementDistressIndex < domainModel.Constants.CSMinPDIToTreat)
            {
                return new CandidateSelectionResult(false, $"SDI = {Math.Round(segment.SurfaceDistressIndex, 2)}, PDI = {Math.Round(segment.PavementDistressIndex, 2)}: below thresholds ({domainModel.Constants.CSMinSDIToTreat}, {domainModel.Constants.CSMinPDIToTreat})");
            }

            return new CandidateSelectionResult(true, "OK. Passed CSA Checks");        
        }
        catch (Exception ex)
		{
			throw new Exception($"Error checking if {segment.FeebackCode} is a valid Candidate for treatment. Details: {ex.Message}");
		}
    }       

}
