using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            foreach (var strategySetup in _frameworkModel.StrategiesSetupData)
            {
                if (this.MustTriggerStrategy(strategySetup, segment) == true)
                {

                    TreatmentStrategy strategy = new TreatmentStrategy(segment.ElementIndex,numInputs,textInputs,numModParamValues,textModParamValues,period);

                    strategy.AddFirstTreatment(strategySetup.FirstTreatment, segment.AreaSquareMetre, "no comment", "no reason", strategySetup.ForceFirstTreatment);

                    if (strategySetup.Treat2Name != string.Empty)
                    {
                        strategy.AddFollowUpTreatment(strategySetup.Treat2Name, strategySetup.Treat2WaitPeriod, segment.AreaSquareMetre, "no comment", "no reason", strategySetup.Treat2Force);
                    }

                    if (strategySetup.Treat3Name != string.Empty)
                    {
                        strategy.AddFollowUpTreatment(strategySetup.Treat3Name, strategySetup.Treat3WaitPeriod, segment.AreaSquareMetre, "no comment", "no reason", strategySetup.Treat3Force);
                    }

                    if (strategySetup.Treat4Name != string.Empty)
                    {
                        strategy.AddFollowUpTreatment(strategySetup.Treat4Name, strategySetup.Treat4WaitPeriod, segment.AreaSquareMetre, "no comment", "no reason", strategySetup.Treat4Force);
                    }

                    strategies.Add(strategy);

                }
            }

            return strategies;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error generating candidate strategies for segment {segment.FeebackCode} in period {period}", ex);            
        }        
    }

    private bool MustTriggerStrategy(StrategySetupInfo strategySetupInfo, RoadSegment segment)
    {
        string firstTreatment = strategySetupInfo.FirstTreatment.ToLower();
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
