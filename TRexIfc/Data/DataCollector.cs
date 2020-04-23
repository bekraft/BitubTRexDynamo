using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Bitub.Ifc.Scene;
using Bitub.Ifc.Transform.Requests;

using Google.Protobuf;

using Autodesk.DesignScript.Runtime;

namespace Data
{
    /// <summary>
    /// IFC aggregated helper utilities.
    /// </summary>
    public class DataCollector
    {
        #region Internal

        internal DataCollector()
        {
        }

        #endregion

        internal static string[] CollectPropertySets(Store.IfcStore ifcStore)
        {
            return ifcStore.XbimModel.Instances
                        .OfType<IIfcPropertySetDefinition>()
                        .Select(s => s.Name?.ToString())
                        .Distinct()
                        .ToArray();
        }

        /// <summary>
        /// Gets all property set names in distinct order.
        /// </summary>
        /// <param name="storeProducer">IFC model producer</param>
        /// <returns>A unique sequence of property set names</returns>
        public static string[][] CollectPropertySetNames(Store.IfcStoreProducer storeProducer)
        {
            List<string[]> namesPerModel = new List<string[]>();
            foreach(var store in storeProducer)
            {
                namesPerModel.Add(CollectPropertySets(store));
            }

            return namesPerModel.ToArray();
        }
        
        /// <summary>
        /// Exports an IFC model as binary Scene model.
        /// </summary>
        /// <param name="fileNames"></param>
        /// <returns>Exported model file names</returns>
        [MultiReturn(new[] { "sceneFiles", "failures" })]
        public static Dictionary<string, string[]> ExportBinSceneModel(string[] fileNames)
        {
            List<string> outFileNames = new List<string>();
            List<string> failures = new List<string>();
            var exporter = new IfcSceneExporter(new XbimTesselationContext());
            exporter.Settings = new IfcSceneExportSettings
            {
                Positioning = ScenePositioningStrategy.NoCorrection,
                Transforming = SceneTransformationStrategy.Quaternion                
            };
            foreach (var fileName in fileNames)
            {                
                using (var model = Xbim.Ifc.IfcStore.Open(fileName))
                {
                    try
                    {
                        var sceneTask = exporter.Run(model);
                        sceneTask.Wait();

                        var sceneFileName = $@"{Path.GetDirectoryName(fileName)}\{Path.GetFileNameWithoutExtension(fileName)}.scene";
                        outFileNames.Add(sceneFileName);
                        using (var binStream = File.Create(sceneFileName))
                        {
                            var binScene = sceneTask.Result.Scene.ToByteArray();
                            binStream.Write(binScene, 0, binScene.Length);
                        }
                    }
                    catch (Exception e)
                    {
                        failures.Add($"Failure on '{fileName}': {e.Message}");
                    }
                }
            }

            return new Dictionary<string, string[]>
            {
                { "sceneFiles", outFileNames.ToArray() },
                { "failures", failures.ToArray() }
            };
        }
    }
}
