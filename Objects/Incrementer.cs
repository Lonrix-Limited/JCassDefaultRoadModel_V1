using JCass_ModelCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCassDefaultRoadModel.Objects;

public class Incrementer
{

    private ModelBase _frameworkModel;
    private RoadNetworkModel _domainModel;

    public Incrementer(ModelBase frameworkModel, RoadNetworkModel domainModel)
    {
        _frameworkModel = frameworkModel ?? throw new ArgumentNullException(nameof(frameworkModel), "Domain model cannot be null");
        _domainModel = domainModel ?? throw new ArgumentNullException(nameof(domainModel), "Domain model cannot be null");
    }

}
