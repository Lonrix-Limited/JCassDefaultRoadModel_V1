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




            return strategies;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error generating candidate strategies for segment {segment.FeebackCode} in period {period}", ex);            
        }        
    }


    private void AddRehabStrategies(RoadSegment segment, int period, List<TreatmentStrategy> strategies,

        Dictionary<string, object> infoFromModel,
        Dictionary<string, double> numInputs, Dictionary<string, string> textInputs,
        Dictionary<string, double> numModParamValues, Dictionary<string, string> textModParamValues)
    {
        TreatmentStrategy strategy = new TreatmentStrategy(segment.ElementIndex, numInputs, textInputs, numModParamValues, textModParamValues, period);
        string treatmentName = "Rehab_" + segment.SurfaceRoadType.ToUpper(); ;
        
        List<int> followUpPeriods = new List<int>() { 8, 12, 14, 16 };
        
        foreach (int iFirst in followUpPeriods)
        {
            strategy.AddFirstTreatment(treatmentName, segment.AreaSquareMetre, "my comment", "my reason", false);
            strategy.AddFollowUpTreatment("ChipSeal_S", 1, segment.AreaSquareMetre, "Second-coat", "none", true);  //Forcing second coat
            strategy.AddFollowUpTreatment("ChipSeal_P", iFirst, segment.AreaSquareMetre, "none", "none", false);       // Do not force follow-up

            foreach (int iSecond in followUpPeriods)
            {
                strategy.AddFollowUpTreatment("ChipSeal_P", iFirst + iSecond, segment.AreaSquareMetre, "none", "none", false);       // Do not force follow-up
            }            

            strategies.Add(strategy);
        }        
    }


    
}
