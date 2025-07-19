using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using JCass_ModelCore.DomainModels;
using JCass_ModelCore.Treatments;

namespace JCassDefaultRoadModel.Objects;

public class RoadNetworkModel : DomainModelBase
{
    public Constants Constants { get; set; }
    
    private Initialiser _initialiser { get; set; }
    private Resetter _resetter { get; set; }
    private Incrementer _incrementer { get; set; }

    public FlushingModel FlushingModel;
    public EdgeBreakModel EdgeBreakModel;
    public ScabbingModel ScabbingModel;
    public LTCracksModel LTCracksModel;    
    public MeshCrackModel MeshCrackModel;
    public ShovingModel ShovingModel;
    public PotholeModel PotholeModel;



    public RoadNetworkModel()
    {
        //Nothing to do here. Note that property 'model' mapping to the ModelBase class (i.e. the Framework Model)
        //will be automatically set up right after this default constructor is called.        
    }

    /// <summary>
    /// Stub that allows custom domain models to set up custom elements such as Machine Learning models,
    /// lookups, special objects etc
    /// </summary>
    public override void SetupInstance(List<string[]> rawData)
    {
        try
        {
            _initialiser = new Initialiser(this.model, this);
            _resetter = new Resetter(this.model, this);
            _incrementer = new Incrementer(this.model, this);
            this.Constants = new Constants(this.model.Lookups);
            this.SetupDistressModels();

        }
        catch (Exception ex)
        {
            // Tell the user where the error occurred
            throw new Exception($"Error setting up custom Road Network Model: {ex.Message}");            
        }        
    }

    private void SetupDistressModels()
    {

        // Get the thresholds for all S-Curve functions from the lookup table
        double aadiMin = this.model.GetLookupValueNumber("distress", "aadi_min");
        double aadiMax = this.model.GetLookupValueNumber("distress", "aadi_max");

        double initValMin = this.model.GetLookupValueNumber("distress", "iv_min");
        double initValMax = this.model.GetLookupValueNumber("distress", "iv_max");
        double initValExpected = this.model.GetLookupValueNumber("distress", "iv_expected");

        // Potholes have lower percentages than other distresses. It has a separate initialisaiton value
        double initValExpectedPotholes = this.model.GetLookupValueNumber("distress", "iv_poth_expected");
        

        double t100Min = this.model.GetLookupValueNumber("distress", "t100_min");
        double t100Max = this.model.GetLookupValueNumber("distress", "t100_max");

        this.FlushingModel = new FlushingModel(this.model);
        FlushingModel.Setup(aadiMin, aadiMax, t100Min, t100Max, initValMin, initValMax, initValExpected);

        this.EdgeBreakModel = new EdgeBreakModel(this.model);
        EdgeBreakModel.Setup(aadiMin, aadiMax, t100Min, t100Max, initValMin, initValMax, initValExpected);

        this.ScabbingModel = new ScabbingModel(this.model);
        ScabbingModel.Setup(aadiMin, aadiMax, t100Min, t100Max, initValMin, initValMax, initValExpected);

        this.LTCracksModel = new LTCracksModel(this.model);
        LTCracksModel.Setup(aadiMin, aadiMax, t100Min, t100Max, initValMin, initValMax, initValExpected);

        this.MeshCrackModel = new MeshCrackModel(this.model);
        MeshCrackModel.Setup(aadiMin, aadiMax, t100Min, t100Max, initValMin, initValMax, initValExpected);

        this.ShovingModel = new ShovingModel(this.model);
        ShovingModel.Setup(aadiMin, aadiMax, t100Min, t100Max, initValMin, initValMax, initValExpected);

        this.PotholeModel = new PotholeModel(this.model);
        PotholeModel.Setup(aadiMin, aadiMax, t100Min, t100Max, initValMin, initValMax, initValExpectedPotholes);

    }

    /// <summary>
    /// Evaluates the Initial Values for all parameters for the element at the start of the analysis. This method is called from the Framework Model 
    /// for all elements at the start of the model run. Use the raw/input data values with domain logic to assign an initial value to all
    /// modelling parameters. 
    /// </summary>
    /// <param name="iElemIndex">Zero-based index of the element</param>    
    /// <param name="rawRow">Input row associated with this element</param>    
    /// <returns>An array of double values representing the actual or encoded values for all model parameters</returns>
    public override double[] Initialise(int iElemIndex, string[] rawRow)
    {
        try
        {
            if (iElemIndex == 752)
            {
                int kk = 9;
            }

            Dictionary<string, object> infoFromModel = model.GetSpecialPlaceholderValues(iElemIndex, rawRow, 0);
            RoadSegment segment = _initialiser.InitialiseSegment(rawRow, iElemIndex);

            // Update the formula values such as PDI, SDI, Objective Value Parameters, Maintenance Cost and CSA Status/Outcome
            // before getting the parameter values
            segment.UpdateFormulaValues(this.model, this, 0,infoFromModel);
            
            Dictionary<string, object> parameterValues = segment.GetParameterValues();

            //Get the initialised values from the updated dictionary and extract the parameter values to return for model parameters
            double[] newValues = this.model.GetModelParameterValuesFromDomainModelResultSet(new double[this.model.NParameters], parameterValues);

            return newValues;  //Return model parameter values for this element
        }
        catch (Exception ex)
        {
            throw new Exception($"Error initialising on element index {iElemIndex}. Details: {ex.Message}");
        }
    }

    /// <summary>
    /// Evaluates the Reset/Updated values for all parameters for the element at the start of the analysis. This method is called from the Framework Model 
    /// for all elements at the start of the model run. Use the raw/input data values with domain logic to assign an initial value to all
    /// modelling parameters. 
    /// </summary>
    /// <param name="iElemIndex">Zero-based index of the element</param>
    /// <param name="iPeriod">Modelling period (values like 1,2,...n)</param>
    /// <param name="rawRow">Input row associated with this element</param>
    /// <param name="prevValues">Double-encoded values for all parameters for this element in the previous epoch</param>
    /// <returns>An array of double values representing the actual or encoded values for all model parameters after Reset is applied</returns>
    public override double[] Reset(TreatmentInstance treatment, int iElemIndex, int iPeriod, string[] rawRow, double[] prevValues)
    {
        try
        {
            Dictionary<string, object> infoFromModel = model.GetParametersForDomainModel(iElemIndex, rawRow, prevValues, iPeriod);

            RoadSegment segment = RoadSegmentFactory.GetFromModel(this.model, infoFromModel, iElemIndex);
            segment.UpdateFormulaValues(this.model, this, iPeriod, infoFromModel);
            

            // Apply Resets
            RoadSegment resettedSegment = _resetter.Reset(segment, iPeriod, treatment);
            resettedSegment.UpdateFormulaValues(this.model, this, iPeriod, infoFromModel);
            

            Dictionary<string, object> parameterValues = resettedSegment.GetParameterValues();

            //Get the initialised values from the updated dictionary and extract the parameter values to return for model parameters
            double[] newValues = this.model.GetModelParameterValuesFromDomainModelResultSet(new double[this.model.NParameters], parameterValues);

            return newValues;  //Return model parameter values for this element
        }
        catch (Exception ex)
        {
            throw new Exception($"Error Resetting element index {iElemIndex}. Details: {ex.Message}");
        }
    }

    /// <summary>
    /// Evaluates the Increment for all parameters for the element in the current period. This method is called from the Framework Model 
    /// for elements that do not have a treatment selected after optimisation in the current period. 
    /// </summary>
    /// <param name="iElemIndex">Zero-based index of the element</param>
    /// <param name="iPeriod">Modelling period (values like 1,2,...n)</param>
    /// <param name="rawRow">Input row associated with this element</param>
    /// <param name="prevValues">Double-encoded values for all parameters for this element in the previous epoch</param>
    /// <returns>An array of double values representing the actual or encoded values for all model parameters after the Increment is applied</returns>
    public override double[] Increment(int iElemIndex, int iPeriod, string[] rawRow, double[] prevValues)
    {
        try
        {
            Dictionary<string, object> infoFromModel = model.GetParametersForDomainModel(iElemIndex, rawRow, prevValues, iPeriod);

            RoadSegment segment = RoadSegmentFactory.GetFromModel(this.model, infoFromModel, iElemIndex);
            segment.UpdateFormulaValues(this.model, this, iPeriod, infoFromModel);
            segment.UpdateCandidateSelectionResult(this.model, this, 0, infoFromModel);

            // Apply increments here
            RoadSegment incrementedSegment = _incrementer.Increment(segment, iPeriod);
            incrementedSegment.UpdateFormulaValues(this.model, this, iPeriod, infoFromModel);

            Dictionary<string, object> parameterValues = incrementedSegment.GetParameterValues();

            //Get the initialised values from the updated dictionary and extract the parameter values to return for model parameters
            double[] newValues = this.model.GetModelParameterValuesFromDomainModelResultSet(new double[this.model.NParameters], parameterValues);

            return newValues;  //Return model parameter values for this element
        }
        catch (Exception ex)
        {
            throw new Exception($"Error Incrementing element index {iElemIndex}. Details: {ex.Message}");
        }
    }

    /// <summary>
    /// Get alll stategies that can be considered for this element in the Benefit-Cost Analysis model. This method is called from
    /// the Framwework Model only if the model type is Benefit-Cost Analysis (BCA). The strategies returned by this method will be 
    /// evaluated over the look-ahead period and then combined with stategies for all other elements in the optimisation stage.
    /// </summary>
    /// <param name="iElemIndex">Zero-based index of the element</param>
    /// <param name="iPeriod">Modelling period (values like 1,2,...n)</param>
    /// <param name="rawRow">Input row associated with this element</param>
    /// <param name="prevValues">Double-encoded values for all parameters for this element in the previous epoch</param>
    /// <returns>List of Treatment Strategies to consider for this element</returns>
    public override List<TreatmentStrategy> GetStrategies(int iElemIndex, int iPeriod, string[] rawRow, double[] prevValues)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Execute treatment selection/trigger logic to select all treatment instances for an element in the current period. The
    /// framework model will call this method for each element in and for each period. This method is only used in MCDA type models
    /// that evaluate individual treatments instead of Strategies. Use the raw input values for
    /// the element as well as the previous values of the parameters for the element with your domain logic to determine which
    /// treatment(s) can be considered for this element in the optimisation stage. If no treatments are applicable, return an empty list.
    /// </summary>
    /// <param name="iElemIndex">Zero-based index of the element</param>
    /// <param name="iPeriod">Modelling period (values like 1,2,...n)</param>
    /// <param name="rawRow">Input row associated with this element</param>
    /// <param name="prevValues">Double-encoded values for all parameters for this element in the previous epoch</param>
    /// <returns>A list of all treatment instances to consider for this element in the optimisation stage</returns>
    public override List<TreatmentInstance> GetTreatmentCandidates(int iElemIndex, int iPeriod, string[] rawRow, double[] prevValues)
    {
        try
        {
            if (iElemIndex == 752 && iPeriod == 1)
            {
                int kk = 9;
            }

            Dictionary<string, object> infoFromModel = model.GetParametersForDomainModel(iElemIndex, rawRow, prevValues, iPeriod);

            RoadSegment segment = RoadSegmentFactory.GetFromModel(this.model, infoFromModel, iElemIndex);            
            //segment.UpdateCandidateSelectionResult(this.model, this, iPeriod, infoFromModel);
            //segment.UpdateFormulaValues(this.model, this, iPeriod, infoFromModel);  //Immediately update the formula values for the segment

            TreatmentsTriggerMCDA mcdaTriggerFunction = new TreatmentsTriggerMCDA(this.model, this);
            List<TreatmentInstance> candidates = mcdaTriggerFunction.GetTriggeredTreatments(segment, iPeriod, infoFromModel);

            return candidates;

        }
        catch (Exception ex)
        {
            throw new Exception($"Error checking Treatment Candidate Selection on element index {iElemIndex}. Details: {ex.Message}");
        }
    }

    /// <summary>
    /// Uses domain logic to determine if there is routine maintenance triggered for the current element and period. This method is 
    /// called from the Framework Model after treatment selection to determine if there is any triggered maintenance that should be applied to the element.
    /// If there is no triggered maintenance, return null.
    /// </summary>
    /// <param name="iElemIndex">Zero-based index of the element</param>
    /// <param name="iPeriod">Modelling period (values like 1,2,...n)</param>
    /// <param name="rawRow">Input row associated with this element</param>
    /// <param name="prevValues">Double-encoded values for all parameters for this element in the previous epoch</param>
    /// <returns>A Treatment Instance object representing Routine Maintenance</returns>
    public override TreatmentInstance GetTriggeredMaintenance(int iElemIndex, int iPeriod, double[] paramValues, string[] rawData)
    {
        try
        {
            Dictionary<string, object> infoFromModel = model.GetParametersForDomainModel(iElemIndex, rawData, paramValues, iPeriod);

            RoadSegment segment = RoadSegmentFactory.GetFromModel(this.model, infoFromModel, iElemIndex);
            segment.UpdateFormulaValues(this.model, this, iPeriod, infoFromModel);  //Immediately update the formula values for the segment

            return RoutineMaintenance.GetRoutineMaintenance(segment, iPeriod);

        }
        catch (Exception ex)
        {
            throw new Exception($"Error triggering Routine Maintenance on element index {iElemIndex}. Details: {ex.Message}");
        }
    }

    
        

    /// <summary>
    /// Omit this method. It is deprecated.
    /// </summary>
    /// <param name="iElemIndex"></param>
    /// <param name="rawRow"></param>
    /// <returns></returns>
    public override double[] InitialiseForCalibration(int iElemIndex, string[] rawRow)
    {
        throw new NotImplementedException();
    }


    




}
