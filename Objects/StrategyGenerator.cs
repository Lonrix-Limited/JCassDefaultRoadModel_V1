using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using JCass_ModelCore.Models;
using JCass_ModelCore.Treatments;
using JCassDefaultRoadModel.Objects;

namespace JCassDefaultRoadModelV1.Objects;

public class StrategyGenerator
{

    private ModelBase _frameworkModel;
    private RoadNetworkModel _domainModel;

    public StrategyGenerator(ModelBase frameworkModel, RoadNetworkModel domainModel)
    {
        _frameworkModel = frameworkModel ?? throw new ArgumentNullException(nameof(frameworkModel), "Domain model cannot be null");
        _domainModel = domainModel ?? throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
    }

    public List<TreatmentStrategy> GetCandidateStrategies(RoadSegment segment, int period, 
        Dictionary<string, object> infoFromModel,
        Dictionary<string, double> numInputs, Dictionary<string, string> textInputs,
        Dictionary<string, double> numModParamValues, Dictionary<string, string> textModParamValues)
    {
        try
        {
            List<TreatmentStrategy> strategies = new List<TreatmentStrategy>();

            if (segment.ElementIndex == 522 && period >= 0)
            {
                int debug = 0;
            }

            // Check if the segment passes the Candidate Selection checks. If not, return an empty list.
            if (segment.IsCandidateForTreatment == 0) return strategies;

            // Although we check if Periods to Next Treatment (i.e. committed) in the Candidate Selection, we need to do it 
            // again here, because the Candidate Selection result was last evaluated at the last epoch, while the periods to
            // next treatment have now changed since the period has changed
            int periodsToNextTreatment = Convert.ToInt32(infoFromModel["periods_to_next_treatment"]);
            if (periodsToNextTreatment <= 6) { return strategies; }

            int istrat = 0;
            foreach (var strategySetup in _frameworkModel.StrategiesSetupData)
            {
                if( istrat == 41)
                {
                    int debug = 0;
                }
                try
                {
                    if (this.MustTriggerStrategy(strategySetup, segment) == true)
                    {

                        TreatmentStrategy strategy = new TreatmentStrategy(segment.ElementIndex, strategySetup.StrategyName, numInputs, textInputs, numModParamValues, textModParamValues, period);
                        strategy.Key = strategySetup.StrategyName;

                        TreatmentInstance firstTreatment = new TreatmentInstance(segment.ElementIndex, strategySetup.FirstTreatment, period,
                                segment.AreaSquareMetre, strategySetup.ForceFirstTreatment, "no comment", "no reason");
                        strategy.AddFirstTreatment(firstTreatment);

                        if (strategySetup.Treat2Name != string.Empty)
                        {
                            //Note: Wait period will be automatically adjusted by adding 1 to the wait period - because the first treatment is done in the current period
                            TreatmentInstance followUpTreatment2 = new TreatmentInstance(segment.ElementIndex, strategySetup.Treat2Name, strategySetup.Treat2WaitPeriod,
                                segment.AreaSquareMetre, strategySetup.Treat2Force, "no comment", "no reason");
                            strategy.AddFollowUpTreatment(followUpTreatment2, strategySetup.Treat2WaitPeriod);
                        }

                        if (strategySetup.Treat3Name != string.Empty)
                        {
                            //Note: Wait period will be automatically adjusted by adding 1 to the wait period - because the first treatment is done in the current period
                            TreatmentInstance followUpTreatment3 = new TreatmentInstance(segment.ElementIndex, strategySetup.Treat3Name, strategySetup.Treat3WaitPeriod,
                                segment.AreaSquareMetre, strategySetup.Treat3Force, "no comment", "no reason");
                            strategy.AddFollowUpTreatment(followUpTreatment3, strategySetup.Treat3WaitPeriod);
                        }

                        if (strategySetup.Treat4Name != string.Empty)
                        {
                            //Note: Wait period will be automatically adjusted by adding 1 to the wait period - because the first treatment is done in the current period
                            TreatmentInstance followUpTreatment4 = new TreatmentInstance(segment.ElementIndex, strategySetup.Treat4Name, strategySetup.Treat4WaitPeriod,
                                segment.AreaSquareMetre, strategySetup.Treat4Force, "no comment", "no reason");
                            strategy.AddFollowUpTreatment(followUpTreatment4, strategySetup.Treat4WaitPeriod);
                        }

                        strategies.Add(strategy);

                        // if the first treatment is forced, then do not add any more strategies
                        if (strategySetup.ForceFirstTreatment == true)
                        {
                            break;
                        }
                    }
                    istrat++;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error setting up strategy for '{strategySetup.StrategyName}'; Details: {ex.Message}");
                }                
            }

            return strategies;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generating candidate strategies for segment {segment.FeebackCode} in period {period}; Details: {ex.Message}");             
        }        
    }

    private bool MustTriggerStrategy(StrategySetupInfo strategySetupInfo, RoadSegment segment)
    {
        string firstTreatment = strategySetupInfo.FirstTreatment.ToLower();

        // If first treatment is a Second Coat and Surface Function is NOT "1" then do not trigger strategy
        if (firstTreatment == "chipseal_s" && segment.SurfaceFunction != "1")
        {
            return false;
        }

        if (firstTreatment == "chipseal_h" && segment.SurfaceFunction != "1a")
        {
            return false;
        }
        else
        {
            if (firstTreatment == "chipseal_h" && segment.SurfaceFunction == "1a")
            {
                int kk = 0;
            }            
        }

        // TODO: Get clarify on surface class names used for Block and Concrete repairs and simplify this logic
        // If the surface class is CS or AC, we cannot do blockrep, concrep, or xtreat      
        if (segment.SurfaceClass == "cs" || segment.SurfaceClass == "ac")
        {
            List<string> otherTreatments = new List<string>() { "blockrep", "concrep", "xtreat" };
            if (otherTreatments.Contains(firstTreatment)) return false;
        }


        if (firstTreatment.Contains("seal") && segment.NextSurfaceIsChipSeal == false)
        {
            // We need an AC, so cannot do a seal
            return false;
        }

        if (firstTreatment.Contains("rehab_cs") && segment.NextSurfaceIsChipSeal == false)
        {
            // We need an AC, so cannot do a seal rehab
            return false;
        }

        if (firstTreatment.Contains("thinac") && segment.NextSurfaceIsChipSeal == true)
        {
            // We need a seal, so cannot do a thin AC
            return false;
        }

        if (firstTreatment.Contains("rehab_ac") && segment.NextSurfaceIsChipSeal == true)
        {
            // We need a seal, so cannot do a thin AC rehab
            return false;
        }

        if (firstTreatment.StartsWith("rehab"))
        {
            if (segment.CanRehabFlag == false)
            {
                // Cannot rehab this segment
                return false;
            }

            string roadClass = segment.RoadType.ToLower();
            if (segment.NextSurfaceIsChipSeal == true)
            {
                string validRehab = "rehab_cs_" + roadClass;
                if (firstTreatment != validRehab)
                {
                    // Invalid rehab treatment for this road class
                    return false;
                }
            }

            if (segment.NextSurfaceIsChipSeal == false)
            {
                string validRehab = "rehab_ac_" + roadClass;
                if (firstTreatment != validRehab)
                {
                    // Invalid rehab treatment for this road class
                    return false;
                }
            }
        }

        // If we get here, all checks passed. So strategy must be triggered
        return true;

    }






}
