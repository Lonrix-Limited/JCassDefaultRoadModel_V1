using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JCass_Functions.Lookups;
using JCassDefaultRoadModel.Objects;

namespace JCassDefaultRoadModel.LookupObjects;

public class LookupUtility
{

    private JFuncLookupNumber RoadClassLookup { get; set; }


    public string GetRoadClass(string onrc)
    {
        Dictionary<string, object> values = new Dictionary<string, object> {  { "file_onrc", onrc } };
        return RoadClassLookup.Evaluate(values).ToString();
    }

    public LookupUtility(Dictionary<string, Dictionary<string, object>> lookups)
    {
        this.RoadClassLookup = new JFuncLookupNumber("road_class: file_onrc : default", lookups);
               

    }


}
