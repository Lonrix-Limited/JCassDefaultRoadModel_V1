using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JCass_ModelCore.DomainModels;
using JCass_ModelCore.Treatments;
using JCassDefaultRoadModel.Initialisation;
using JCassDefaultRoadModel.LookupObjects;

namespace JCassDefaultRoadModel.Objects;

public class RoadNetworkModel : DomainModelBase
{
    public GeneralConstants Constants { get; set; }
    public LookupUtility LookupUtil { get; set; }

    private Initialiser _initialiser { get; set; }


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
            this.Constants = new GeneralConstants(this.model.Lookups);

            foreach (var row in rawData)
            {
                RoadSegment seg = RoadSegmentFactory.GetFromRawData(this.model, row, this.LookupUtil);
            }

        }
        catch (Exception ex)
        {
            // Tell the user where the error occurred
            throw new Exception($"Error setting up custom Road Network Model: {ex.Message}");            
        }        
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
            RoadSegment segment = _initialiser.InitialiseSegment(rawRow);
            return null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error initialising on element index {iElemIndex}");
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    

}
