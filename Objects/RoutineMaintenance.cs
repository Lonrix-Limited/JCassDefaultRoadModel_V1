using JCass_ModelCore.Treatments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.Objects;

public static class RoutineMaintenance
{

    public static TreatmentInstance GetRoutineMaintenance(RoadSegment segment, int period)
    {
        // Note that maintenance cost calculation already checks if the segment is AC or Chipseal
        // and that the PDI is over the threshold specified for maintenance in lookups
        if (segment.MaintenanceCostPerKm <= 0) { return null; }
     
        double cost = segment.MaintenanceCostPerKm * (segment.LengthInMetre / 1000);
        double quantity = cost / 1.0;  //Unit rate is 1.0
        string reason = "Routine Maintenance";
        string comment = $"PDI = {Math.Round(segment.PavementDistressIndex, 2)}; Rut = M{Math.Round(segment.RutParameterValue,2)}mm";
        TreatmentInstance routMaint = new TreatmentInstance(segment.ElementIndex, "RMaint", period, quantity, false, reason, comment);
        return routMaint;

    }


}
