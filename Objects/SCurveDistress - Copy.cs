using JCass_Functions.Engineering;
using JCass_ModelCore.DomainModels;
using JCass_ModelCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NPOI.SS.Formula.PTG.ArrayPtg;

namespace JCassDefaultRoadModel.Objects;

/// <summary>
/// Base class for prediction of distresses that follow an S-curve pattern over time.
/// </summary>
public abstract class SCurveDistress
{

    protected PieceWiseLinearModelGeneric _resetCurveForCS;

    protected PieceWiseLinearModelGeneric _resetCurveForAC;

    protected ModelBase _frameworkModel;

    /// <summary>
    /// Maximum threshold for AADI, which is the Age At Distress Initiation
    /// </summary>
    protected double _AADIMax;

    /// <summary>
    /// Minimum threshold for AADI, which is the Age At Distress Initiation
    /// </summary>
    protected double _AADIMin;


    /// <summary>
    /// Maximum threshold for T100, which is the time it takes for the distress to reach 100% of segment area.
    /// </summary>
    protected double _T100Max;

    /// <summary>
    /// Minimum threshold for T100, which is the time it takes for the distress to reach 100% of segment area.
    /// </summary>
    protected double _T100Min;

    /// <summary>
    /// Maximum threshold for the Initial Value, which is the initial percent distress right after AADI is reached.
    /// </summary>
    protected double _InitValMax;

    /// <summary>
    /// Minimum threshold for the Initial Value, which is the initial percent distress right after AADI is reached.
    /// </summary>
    protected double _InitValMin;

    /// <summary>
    /// Expected value for the initial value for the distress right after AADI is reached. 
    /// </summary>
    protected double _InitValExpected = 0.5;


    /// <summary>
    /// Limit BELOW which even a resurfacing will fully reset aadi. Thus if distress before reset is below this
    /// limit, the AADI will be fully reset based on Surface Expected life and distress probability.
    /// </summary>
    protected double _resetLimit1 = 0.0;

    /// <summary>
    /// Limit ABOVE which aadi is set to 1. Thus distress starts again first year after treatment if the distress
    /// before treatment is above this limit
    /// </summary>
    protected double _resetLimit2 = 10.0;

    /// <summary>
    /// Multiplier to increase T100 estimated from current percent and surface age, to take account of resurfacing benefits
    /// </summary>
    protected double _resetLimit3 = 30.0;

    /// <summary>
    /// Boost in AADI when a holding action is performed over a Surface Seal. This value is to be extracted in Setup from lookup
    /// tables for each inherited distress
    /// </summary>
    protected double _AadiBoostForHoldingAction;


    protected SCurveDistress(ModelBase model)
    {
        _frameworkModel = model;
    }

    public abstract void Setup(double aadiMin, double aadiMax, double t100Min, double t100Max, double ivMin, double ivMax, double initialValueExpected = 0.5);

    /// <summary>
    /// Gets the initial value for a distress type based on the segment's condition survey date and current value. If the 
    /// condition survey is outdated, we establish whether a Rehabilitation was performed or just a Resurfacing. If a rehabilitation
    /// was performed, we return a reset value of 0. If only a resurfacing was performed, we return the reset value for the distress 
    /// type based on the surface class and the PiecewiseLinear function for the distress mapping pre-treatment to post-treatment values (depending
    /// on surface class).
    /// </summary>
    /// <param name="segment">Road Segment</param>
    /// <param name="currentValue">Current value</param>
    /// <param name="baseDate">Base date used in the model, so that we can establish survey age using the same basis as Surface age</param>
    /// <returns></returns>
    /// <exception cref="Exception">An exception is thrown if the surface class is not 'ac' or 'cs'</exception>
    public double GetInitialValue(RoadSegment segment, double currentValue, DateTime baseDate)
    {
        DateTime surveyDate = JCass_Core.Utils.HelperMethods.ParseDateNoTime(segment.ConditionSurveyDateString);
        double age = (baseDate - surveyDate).TotalDays / 365.25; // Use 365.25 to account for leap years        
        if (age < 0) 
        { 
            _frameworkModel.LogMessage($"Distress Condition Survey date for segment {segment.FeebackCode} is in the future", false); 
        }

        bool dataIsOutdated = segment.SurfaceAge < age;
        if (dataIsOutdated)
        {
            // If the a surfacing has been done since the survey and surface function is '1', then there was 
            // a Rehabilitation, thus we assume value has been completely reset
            if (segment.SurfaceFunction == "1") return 0.0;

            // If the surface function is '2' or 'R', then we assume there was only a resurfacing, so return the
            // reset value for the distress type
            if (segment.SurfaceClass == "cs")
            {
                return _resetCurveForCS.GetValue(currentValue);
            }
            else if (segment.SurfaceClass == "ac")
            {
                return _resetCurveForAC.GetValue(currentValue);
            }
            else
            {
                throw new Exception($"Unknown surface class {segment.SurfaceClass} for segment {segment.FeebackCode}");
            }
        }
        else
        {
            // If the data is not outdated, we assume the distress value is still valid
            return currentValue;
        }

    }

    public abstract double DistressProbability(RoadSegment segment);

    /// <summary>
    /// Utility function to calculate the Logit function on a value, where the function is
    /// defined as 'Math.Exp(value) / (1 + Math.Exp(value))'
    /// </summary>
    /// <param name="value">Value on which to calculate logic</param>
    /// <returns></returns>
    protected double Logit(double value)
    {
        return Math.Exp(value) / (1 + Math.Exp(value));
    }

    /// <summary>
    /// Gets the Age At Distress Initiation (AADi) expected value for a given road segment. This is calculated as the expected life of the surface
    /// multiplied by 1 minus the probability of distress for the segment. Thus the larger the probability of distress, the smaller the expected AADi value.
    /// </summary>        
    protected double GetAadiExpectedValue(double expectedSurfaceLife, double distressProb)
    {
        double value = expectedSurfaceLife * (1.0 - distressProb);
        // Ensure the value is within the defined AADI limits
        return Math.Clamp(value, _AADIMin, _AADIMax);
    }

    /// <summary>
    /// Gets the T100 expected value for a given road segment. This is calculated as Maximum Threshold for T100
    /// multiplied by 1 minus the probability of distress for the segment. Thus the larger the probability of distress, the smaller the expected T100 value.
    /// </summary>    
    /// <remarks>For details see: https://lonrix-limited.github.io/jcass_docs2/jfuncs/jfunc_s_calibrator.html</remarks>
    protected double GetT100ExpectedValue(double distressProbability)
    {
        return _T100Max * (1.0 - distressProbability);
    }

    /// <summary>
    /// Calibrates the values for AADI, T100 and Initial Value based on the observed value for the distress type. 
    /// </summary>    
    /// <param name="observedValue">Current observed value for the distress</param>    
    /// <returns>Values in concatenated string with [AADI_InitialValue_T100]</returns>
    public string GetCalibratedInitialSetupValues(RoadSegment segment, double observedValue, double errorTolerance)
    {
        //Debug for specific element. Change element index as needed
        if (segment.ElementIndex == 123)
        {
            int kk = 9;   //put breakpoint on this line
        }

        double surfaceExpectedLife = segment.SurfaceExpectedLife;
        double distressProb = this.DistressProbability(segment);

        double aadiExpected = this.GetAadiExpectedValue(surfaceExpectedLife, distressProb);
        double t100Expected = this.GetT100ExpectedValue(distressProb);
        double initialValueExpected = _InitValExpected; // make a copy - do not modify the base value!

        //Check that the expected values (calculated based on probability or some other means) are within the
        //limits. If they are outside of the limits, then clamp then a little higher or lower than the min or max, respectively
        aadiExpected = this.GetVirtualClampValue(aadiExpected, _AADIMin, _AADIMax);
        t100Expected = this.GetVirtualClampValue(t100Expected, _T100Min, _T100Max);
        initialValueExpected = this.GetVirtualClampValue(initialValueExpected, _InitValMin, _InitValMax);

        SShapedModelHelper.CalibrateFactors(segment.SurfaceAge, observedValue, _T100Min, _T100Max, _AADIMin, _AADIMax, _InitValMin, _InitValMax,
            ref t100Expected, ref aadiExpected, ref initialValueExpected, errorTolerance);

        return $"{Math.Round(aadiExpected,2)}_{Math.Round(initialValueExpected,2)}_{Math.Round(t100Expected,2)}";
    }

    private double GetVirtualClampValue(double value, double min, double max)
    {
        if (value <= min)
        {
            return min * 1.05;    //Return value 5% above minimum
        }
        else if (value >= max)
        {
            return max * 0.95;  //Return value 5% below maximum
        }
        return value;
    }

    /// <summary>
    /// Gets the values for AADI, T100 and Initial value after a reset, based on the current value of the distress and the treatment category.
    /// </summary>    
    /// <param name="currentValue">Value for the distress before Treatment is applied</param>
    /// <param name="treatmentCategory">Treatment Category. We expect Rehabilitation treatments to start with 'rehab' and holding treatments
    /// category to start with 'holding' (not case sensitive). All other treatments are presume to be Resurfacings.</param>
    /// <returns>Values in concatenated string with [AADI_InitialValue_T100]</returns>
    public virtual string GetResettedSetupValues(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        treatmentCategory = treatmentCategory.ToLower();

        double surfaceExpectedLife = segment.SurfaceExpectedLife;
        double distressProb = this.DistressProbability(segment);

        // Expected new values for AADI and T100 
        double aadiExpected = this.GetAadiExpectedValue(surfaceExpectedLife, distressProb);
        double resetT100 = _T100Max * (1 - distressProb);

        // For Resurfacing, get the resetted AADI based on the current value (distress before reset) and the
        // reset limiting values from lookups
        string setupCode = $"{_resetLimit1},{aadiExpected}|{_resetLimit2},1";
        PieceWiseLinearModelGeneric interpolator = new PieceWiseLinearModelGeneric(setupCode, false);
        double aadiResurfacing = interpolator.GetValue(currentValue);

        //Approximate previous distress progression, per year, based on Age and Distress % before reset (add 1 to avoid division by zero)
        double previousRate = currentValue / (1 + segment.SurfaceAgeBeforeReset);

        //T100 based on approximate increase in distress % per year before reset
        double t100Pre = 100 / previousRate;

        //Adjusted T100 based on approximate increase in distress % per year before reset, multiplied by adjustment/improvement factor
        double t100Adjusted = t100Pre * _resetLimit3;

        //Final, clamped value for T100 reset in case of resurfacing
        t100Adjusted = Math.Clamp(t100Adjusted, _T100Min, _T100Max);

        //For Resurfacing, adjust T100 for based on lookup limits and value before treatment
        setupCode = $"{_resetLimit1},{resetT100}|{_resetLimit2},{t100Adjusted}";
        interpolator = new PieceWiseLinearModelGeneric(setupCode, false);
        double t100Resurfacing = interpolator.GetValue(currentValue);

        string resetCode = "";
        if (treatmentCategory.Contains("rehab"))
        {
            resetCode = $"{Math.Round(aadiExpected,2)}_{Math.Round(_InitValExpected,2)}_{Math.Round(resetT100,2)}";
        }
        else if (treatmentCategory.Contains("holding"))
        {
            //Number of years added to AADI because of improvements in holding action            
            double aadiForHolding = aadiResurfacing + _AadiBoostForHoldingAction;

            // Ensure that AADI for holding action is not less than allowed minimum or more than value for Rehab (expected AADI)
            aadiForHolding = Math.Clamp(aadiForHolding, _AADIMin, aadiExpected);

            resetCode = $"{Math.Round(aadiForHolding,2)}_{Math.Round(_InitValExpected,2)}_{Math.Round(t100Resurfacing,2)}";
        }
        else
        {
            //Assume Resurfacing
            resetCode = $"{Math.Round(aadiResurfacing,2)}_{Math.Round(_InitValExpected,2)}_{Math.Round(t100Resurfacing,2)}";
        }

        return resetCode;
    }


    protected double GetIncrement(RoadSegment segment, double currentValue, string sCurveSetupCode)
    {
        // Parse the setup code values 
        string[] parts = sCurveSetupCode.Split('_');
        if (parts.Length != 3)
        {
            throw new ArgumentException("Invalid setup code format. Expected format: AADI_InitialValue_T100");
        }

        // Parse the values from the setup code
        double aadi = double.Parse(parts[0]);
        double initialValue = double.Parse(parts[1]);
        double t100 = double.Parse(parts[2]);

        //Case where Age At Distress Initiation is not yet reached - distress remains zero - return zero increment
        if (segment.SurfaceAge < aadi) { return 0.0; }

        //Case where current age is within one period of AADI, meaning that initiation happened in the last period
        //In this case return the expected initial value
        if (segment.SurfaceAge - 1 < aadi) return initialValue;

        //Case where distress is already initialised and somewhere on the progression curve. Return the increment
        //at this stage of the progression curve
        double periodsSinceInitialisation = segment.SurfaceAge - aadi;
        double increm = SShapedModelHelper.GetSCurveProgressionIncrement(t100, periodsSinceInitialisation);
        return increm;

    }

    /// <summary>
    /// Get the next value after applying the increment to the current value of the distress for a given road segment. If surface
    /// age is not yet past AADI, the increment is zero, and the next value remains the same as the current value. 
    /// </summary>    
    /// <param name="currentValue">Current Value for the distress</param>
    /// <param name="sCurveSetupCode">Setup code for the distress S-Curve. Should be current value for distress S-curve info parameter</param>
    /// <returns></returns>
    public double GetNextValueAfterIncrement(RoadSegment segment, double currentValue, string sCurveSetupCode)
    {
        // Only apply increment for Chipseal and Asphalt
        if (segment.SurfaceIsChipSealOrACFlag != 1) return currentValue;

        // Get the increment based on the segment's age and the S-Curve setup code
        double increment = this.GetIncrement(segment, currentValue, sCurveSetupCode);

        // Calculate the next value after applying the increment
        double nextValue = currentValue + increment;

        return nextValue;
    }

    public abstract double GetValueAfterReset(RoadSegment segment, double currentValue, string treatmentCategory);

}


public class FlushingModel : SCurveDistress
{

    public FlushingModel(ModelBase model) : base(model)
    {

    }

    public override void Setup(double aadiMin, double aadiMax, double t100Min, double t100Max, double ivMin, double ivMax, double initialValueExpected = 0.5)
    {
        _AADIMin = aadiMin;
        _AADIMax = aadiMax;
        _T100Max = t100Max;
        _T100Min = t100Min;
        _InitValMin = ivMin;
        _InitValMax = ivMax;
        _InitValExpected = initialValueExpected;

        // Initialize the reset curves for Mesh Cracks
        _resetCurveForCS = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,2 | 30,10 | 100,40", false);
        _resetCurveForAC = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,1 | 30,5 | 100,30", false);              
    }

    public override double DistressProbability(RoadSegment segment)
    {
        //logit(-11.95 + 10 * para_surf_cs_flag + -0.37 * pcal_is_urban_flag + 0.05 * para_hcv_risk)
        double value = -11.95
            + (10 * (segment.SurfaceIsChipSealFlag))
            + (-0.37 * (segment.UrbanRural == "u" ? 1 : 0))
            + (0.05 * segment.HCVRisk);
        return Logit(value);
    }

    public override double GetValueAfterReset(RoadSegment segment , double currentValue, string treatmentCategory)
    {
        //All treatments reset distress to zero. However, the S-Curve parameters will change based on the treatment
        // type and the current (before treatment) distress progression. See method: 'GetResettedSetupValues'
        return 0;  
    }

    /// <summary>
    /// Gets the values for AADI, T100 and Initial value after a reset, based on the current value of the distress and the treatment category.
    /// </summary>    
    /// <param name="currentValue">Value for the distress before Treatment is applied</param>
    /// <param name="treatmentCategory">Treatment Category. We expect Rehabilitation treatments to start with 'rehab' and holding treatments
    /// category to start with 'holding' (not case sensitive). All other treatments are presume to be Resurfacings.</param>
    /// <returns>Values in concatenated string with [AADI_InitialValue_T100]</returns>
    public override string GetResettedSetupValues(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        treatmentCategory = treatmentCategory.ToLower();

        double surfaceExpectedLife = segment.SurfaceExpectedLife;
        double distressProb = this.DistressProbability(segment);

        // Expected new values for AADI and T100 
        double aadiExpected = this.GetAadiExpectedValue(surfaceExpectedLife, distressProb);
        double resetT100 = _T100Max * (1 - distressProb);
        
        string resetCode = $"{Math.Round(aadiExpected,2)}_{Math.Round(_InitValExpected,2)}_{Math.Round(resetT100,2)}"; ;
        
        return resetCode;
    }


}

public class EdgeBreakModel : SCurveDistress
{

    public EdgeBreakModel(ModelBase model) : base(model)
    {

    }

    public override void Setup(double aadiMin, double aadiMax, double t100Min, double t100Max, double ivMin, double ivMax, double initialValueExpected = 0.5)
    {
        _AADIMin = aadiMin;
        _AADIMax = aadiMax;
        _T100Max = t100Max;
        _T100Min = t100Min;
        _InitValMin = ivMin;
        _InitValMax = ivMax;
        _InitValExpected = initialValueExpected;

        // Initialize the reset curves for Mesh Cracks
        _resetCurveForCS = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,2 | 30,10 | 100,40", false);
        _resetCurveForAC = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,1 | 30,5 | 100,30", false);
        
    }

    public override double DistressProbability(RoadSegment segment)
    {
        //logit(4 + -1 * pcal_gen_width + -10 * pcal_is_urban_flag)
        double value = 4
            + (-1 * segment.WidthInMetre)
            + (-10 * (segment.UrbanRural == "u" ? 1 : 0));
        return Logit(value);
    }

    public override double GetValueAfterReset(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        //All treatments reset distress to zero. However, the S-Curve parameters will change based on the treatment
        // type and the current (before treatment) distress progression. See method: 'GetResettedSetupValues'
        return 0;
    }

    /// <summary>
    /// Gets the values for AADI, T100 and Initial value after a reset, based on the current value of the distress and the treatment category.
    /// </summary>    
    /// <param name="currentValue">Value for the distress before Treatment is applied</param>
    /// <param name="treatmentCategory">Treatment Category. We expect Rehabilitation treatments to start with 'rehab' and holding treatments
    /// category to start with 'holding' (not case sensitive). All other treatments are presume to be Resurfacings.</param>
    /// <returns>Values in concatenated string with [AADI_InitialValue_T100]</returns>
    public override string GetResettedSetupValues(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        treatmentCategory = treatmentCategory.ToLower();

        double surfaceExpectedLife = segment.SurfaceExpectedLife;
        double distressProb = this.DistressProbability(segment);

        // Expected new values for AADI and T100 
        double aadiExpected = this.GetAadiExpectedValue(surfaceExpectedLife, distressProb);
        double resetT100 = _T100Max * (1 - distressProb);
        
        string resetCode = $"{Math.Round(aadiExpected, 2)}_{Math.Round(_InitValExpected, 2)}_{Math.Round(resetT100, 2)}";

        return resetCode;
    }


}

public class ScabbingModel : SCurveDistress
{

    public ScabbingModel(ModelBase model) : base(model)
    {

    }

    public override void Setup(double aadiMin, double aadiMax, double t100Min, double t100Max, double ivMin, double ivMax, double initialValueExpected = 0.5)
    {
        _AADIMin = aadiMin;
        _AADIMax = aadiMax;
        _T100Max = t100Max;
        _T100Min = t100Min;
        _InitValMin = ivMin;
        _InitValMax = ivMax;
        _InitValExpected = initialValueExpected;

        // Initialize the reset curves for Mesh Cracks
        _resetCurveForCS = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,2 | 30,10 | 100,40", false);
        _resetCurveForAC = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,1 | 30,5 | 100,30", false);
    }

    public override double DistressProbability(RoadSegment segment)
    {
        //logit(-2.89 + 1.71 * para_surf_cs_flag + 0.62 * pcal_is_urban_flag + 0.06 * para_hcv_risk)
        double value = -2.89
            + (1.71 * (segment.SurfaceIsChipSealFlag))
            + (0.62 * (segment.UrbanRural == "u" ? 1 : 0))
            + (0.06 * segment.HCVRisk);
        return Logit(value);
    }

    public override double GetValueAfterReset(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        //All treatments reset distress to zero. However, the S-Curve parameters will change based on the treatment
        // type and the current (before treatment) distress progression. See method: 'GetResettedSetupValues'
        return 0;
    }

    /// <summary>
    /// Gets the values for AADI, T100 and Initial value after a reset, based on the current value of the distress and the treatment category.
    /// </summary>    
    /// <param name="currentValue">Value for the distress before Treatment is applied</param>
    /// <param name="treatmentCategory">Treatment Category. We expect Rehabilitation treatments to start with 'rehab' and holding treatments
    /// category to start with 'holding' (not case sensitive). All other treatments are presume to be Resurfacings.</param>
    /// <returns>Values in concatenated string with [AADI_InitialValue_T100]</returns>
    public override string GetResettedSetupValues(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        treatmentCategory = treatmentCategory.ToLower();

        double surfaceExpectedLife = segment.SurfaceExpectedLife;
        double distressProb = this.DistressProbability(segment);

        // Expected new values for AADI and T100 
        double aadiExpected = this.GetAadiExpectedValue(surfaceExpectedLife, distressProb);
        double resetT100 = _T100Max * (1 - distressProb);

        string resetCode = $"{Math.Round(aadiExpected,2)}_{Math.Round(_InitValExpected,2)}_{Math.Round(resetT100,2)}"; 

        return resetCode;
    }


}

public class LTCracksModel : SCurveDistress
{

    public LTCracksModel(ModelBase model) : base(model)
    {

    }

    public override void Setup(double aadiMin, double aadiMax, double t100Min, double t100Max, double ivMin, double ivMax, double initialValueExpected = 0.5)
    {
        _AADIMin = aadiMin;
        _AADIMax = aadiMax;
        _T100Max = t100Max;
        _T100Min = t100Min;
        _InitValMin = ivMin;
        _InitValMax = ivMax;
        _InitValExpected = initialValueExpected;

        // Initialize the reset curves for Mesh Cracks
        _resetCurveForCS = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,2 | 30,10 | 100,40", false);
        _resetCurveForAC = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,1 | 30,5 | 100,30", false);

        _resetLimit1 = _frameworkModel.GetLookupValueNumber("distress_reset", "lt_cracks_reset_limit1");
        _resetLimit2 = _frameworkModel.GetLookupValueNumber("distress_reset", "lt_cracks_reset_limit2");
        _resetLimit3 = _frameworkModel.GetLookupValueNumber("distress_reset", "lt_cracks_reset_limit3");

        _AadiBoostForHoldingAction = _frameworkModel.GetLookupValueNumber("resets_general", "aadi_boost_holding_lt_cracks");

    }

    public override double DistressProbability(RoadSegment segment)
    {
        //logit(-1.39 + -1.24 * para_surf_cs_flag + 0.82 * pcal_is_urban_flag + 0.09 * para_hcv_risk + 0.03 * para_scabb_pct)
        double value = -1.39
            + (-1.24 * (segment.SurfaceIsChipSealFlag))
            + (0.82 * (segment.UrbanRural == "u" ? 1 : 0))
            + (0.09 * segment.HCVRisk)
            + (0.03 * segment.PctScabbing);
        return Logit(value);
    }

    public override double GetValueAfterReset(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        //All treatments reset distress to zero. However, the S-Curve parameters will change based on the treatment
        // type and the current (before treatment) distress progression. See method: 'GetResettedSetupValues'
        return 0;
    }
}

public class MeshCrackModel : SCurveDistress
{

    public MeshCrackModel(ModelBase model) : base(model)
    {
        
    }

    public override void Setup(double aadiMin, double aadiMax, double t100Min, double t100Max, double ivMin, double ivMax, double initialValueExpected = 0.5)
    {
        _AADIMin = aadiMin;
        _AADIMax = aadiMax;
        _T100Max = t100Max;
        _T100Min = t100Min;
        _InitValMin = ivMin;
        _InitValMax = ivMax;
        _InitValExpected = initialValueExpected;

        // Initialize the reset curves for Mesh Cracks
        _resetCurveForCS = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,2 | 30,10 | 100,40", false);
        _resetCurveForAC = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,1 | 30,5 | 100,30", false);

        _resetLimit1 = _frameworkModel.GetLookupValueNumber("distress_reset", "mesh_cracks_reset_limit1");
        _resetLimit2 = _frameworkModel.GetLookupValueNumber("distress_reset", "mesh_cracks_reset_limit2");
        _resetLimit3 = _frameworkModel.GetLookupValueNumber("distress_reset", "mesh_cracks_reset_limit3");

        _AadiBoostForHoldingAction = _frameworkModel.GetLookupValueNumber("resets_general", "aadi_boost_holding_mesh_cracks");

    }

    public override double DistressProbability(RoadSegment segment)
    {
        //logit(-2.18 + -0.43 * para_surf_cs_flag + 0.28 * pcal_is_urban_flag + 0.12 * para_hcv_risk + 0.02 * para_lt_cracks_pct + 0.01 * para_scabb_pct)
        double value = -2.18 
            + (-0.43 * (segment.SurfaceClass == "cs" ? 1 : 0))
            + (0.28 * (segment.UrbanRural == "u" ? 1 : 0))
            + (0.12 * segment.HCVRisk)
            + (0.02 * segment.PctLongTransCracks)
            + (0.01 * segment.PctScabbing);
        return Logit(value);
    }

    public override double GetValueAfterReset(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        //All treatments reset distress to zero. However, the S-Curve parameters will change based on the treatment
        // type and the current (before treatment) distress progression. See method: 'GetResettedSetupValues'
        return 0;
    }
}

public class ShovingModel : SCurveDistress
{

    public ShovingModel(ModelBase model) : base(model)
    {

    }

    public override void Setup(double aadiMin, double aadiMax, double t100Min, double t100Max, double ivMin, double ivMax, double initialValueExpected = 0.5)
    {
        _AADIMin = aadiMin;
        _AADIMax = aadiMax;
        _T100Max = t100Max;
        _T100Min = t100Min;
        _InitValMin = ivMin;
        _InitValMax = ivMax;
        _InitValExpected = initialValueExpected;

        // Initialize the reset curves for Mesh Cracks
        _resetCurveForCS = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,2 | 30,10 | 100,40", false);
        _resetCurveForAC = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,1 | 30,5 | 100,30", false);

        _resetLimit1 = _frameworkModel.GetLookupValueNumber("distress_reset", "shove_reset_limit1");
        _resetLimit2 = _frameworkModel.GetLookupValueNumber("distress_reset", "shove_reset_limit2");
        _resetLimit3 = _frameworkModel.GetLookupValueNumber("distress_reset", "shove_reset_limit3");

        _AadiBoostForHoldingAction = _frameworkModel.GetLookupValueNumber("resets_general", "aadi_boost_holding_shove");

    }

    public override double DistressProbability(RoadSegment segment)
    {
        //logit(-3.63 + 0.62 * para_surf_cs_flag + 0.31 * pcal_is_urban_flag + 0.08 * para_hcv_risk + 0.03 * para_mesh_cracks_pct + 0.01 * para_scabb_pct)
        double value = -3.63
            + (0.62 * (segment.SurfaceIsChipSealFlag))
            + (0.31 * (segment.UrbanRural == "u" ? 1 : 0))
            + (0.08 * segment.HCVRisk)
            + (0.03 * segment.PctMeshCracks)
            + (0.01 * segment.PctScabbing);
        return Logit(value);
    }

    public override double GetValueAfterReset(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        //All treatments reset distress to zero. However, the S-Curve parameters will change based on the treatment
        // type and the current (before treatment) distress progression. See method: 'GetResettedSetupValues'
        return 0;
    }
}

public class PotholeModel : SCurveDistress
{

    public PotholeModel(ModelBase model) : base(model)
    {

    }

    public override void Setup(double aadiMin, double aadiMax, double t100Min, double t100Max, double ivMin, double ivMax, double initialValueExpected = 0.5)
    {
        _AADIMin = aadiMin;
        _AADIMax = aadiMax;
        _T100Max = t100Max;
        _T100Min = t100Min;
        _InitValMin = ivMin;
        _InitValMax = ivMax;
        _InitValExpected = initialValueExpected;

        // Initialize the reset curves for Mesh Cracks
        _resetCurveForCS = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,2 | 30,10 | 100,40", false);
        _resetCurveForAC = new PieceWiseLinearModelGeneric("0,0 | 2,0 | 10,1 | 30,5 | 100,30", false);

        _resetLimit1 = _frameworkModel.GetLookupValueNumber("distress_reset", "poth_reset_limit1");
        _resetLimit2 = _frameworkModel.GetLookupValueNumber("distress_reset", "poth_reset_limit2");
        _resetLimit3 = _frameworkModel.GetLookupValueNumber("distress_reset", "poth_reset_limit3");

        _AadiBoostForHoldingAction = _frameworkModel.GetLookupValueNumber("resets_general", "aadi_boost_holding_poth");

    }

    public override double DistressProbability(RoadSegment segment)
    {
        //logit(-3 + 1.15 * para_surf_cs_flag + 0.36 * pcal_is_urban_flag + 0.03 * para_hcv_risk + 0.03 * para_shove_pct + 0.02 * para_mesh_cracks_pct + 0.02 * para_scabb_pct)
        double value = -3
            + (1.15 * (segment.SurfaceIsChipSealFlag))
            + (0.36 * (segment.UrbanRural == "u" ? 1 : 0))
            + (0.03 * segment.HCVRisk)
            + (0.03 * segment.PctShoving)
            + (0.02 * segment.PctMeshCracks)
            + (0.02 * segment.PctScabbing);
        return Logit(value);
    }

    public override double GetValueAfterReset(RoadSegment segment, double currentValue, string treatmentCategory)
    {
        //All treatments reset distress to zero. However, the S-Curve parameters will change based on the treatment
        // type and the current (before treatment) distress progression. See method: 'GetResettedSetupValues'
        return 0;
    }
}