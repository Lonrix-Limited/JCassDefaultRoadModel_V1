using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.LookupObjects;

/// <summary>
/// General model constants set up from model Lookup sets.
/// </summary>
public class GeneralConstants
{

    /// <summary>
    /// Base date for the model run. Maps to lookup set "gernal" and setting key "base_date".
    /// </summary>
    public DateTime BaseDate { get; set; }


    public GeneralConstants(Dictionary<string, Dictionary<string, object>> lookupSets)
    {        
        this.BaseDate = JCass_Core.Utils.HelperMethods.ParseDateNoTime(lookupSets["general"]["base_date"]);
    }




}
