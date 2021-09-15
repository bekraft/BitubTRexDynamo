﻿using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Connectors;

using TRex.Export;

using System.Linq;

namespace TRex.UI.Customization
{
#pragma warning disable CS1591
    public class SceneSaveNodeCustomization : CancelableOptionCommandCustomization<SceneSaveNodeModel, Format>
    {
        public SceneSaveNodeCustomization() : base(ProgressOnPortType.OutPorts)
        { }
    }
#pragma warning restore CS1591
}
