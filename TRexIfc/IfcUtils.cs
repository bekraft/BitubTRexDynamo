using System;
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

namespace TRexIfc
{
    /// <summary>
    /// IFC aggregated helper utilities.
    /// </summary>
    public class IfcUtils
    {
        #region Internal

        internal IfcUtils()
        {
        }

        #endregion

        /// <summary>
        /// Gets all property set names in distinct order.
        /// </summary>
        /// <param name="fileNames">IFC model files</param>
        /// <returns>A unique sequence of property set names</returns>
        public static string[][] ListPropertySetNames(string[] fileNames)
        {
            Dictionary<string, string[]> psetNamesPerFile = new Dictionary<string, string[]>();
            foreach (var fileName in fileNames)
            {
                ISet<string> psetNames = new HashSet<string>();
                using (var model = IfcStore.Open(fileName))
                {
                    foreach (string name in model.Instances
                        .OfType<IIfcPropertySetDefinition>()
                        .Select(s => s.Name))
                    {
                        psetNames.Add(name);
                    }
                }
                psetNamesPerFile.Add(Path.GetFileName(fileName), psetNames.ToArray());
            }

            return psetNamesPerFile.Select(e => e.Value).ToArray();
        }

        public static string[] RenamePropertySets(
            string[] fileNames,
            string targetDirectory,
            string[] propertySetNames,
            string[] replaceByNames,
            string replaceFileNamePattern,
            string replaceWith)
        {
            List<string> reports = new List<string>();
            return reports.ToArray();
        }

        /// <summary>
        /// Runs the property set removal
        /// </summary>
        /// <param name="fileNames">IFC files</param>
        /// <param name="targetDirectory">Target directory</param>
        /// <param name="pSetNames">Removal names</param>
        /// <param name="replacePattern">Regular expression replacement pattern in file name</param>
        /// <param name="replaceWith">Replacement</param>
        /// <returns>Result messages</returns>
        public static string[] RemovePropertySets(
            string[] fileNames,
            string targetDirectory,
            string[] pSetNames,
            string replacePattern,
            string replaceWith)
        {
            var editorCredentials = new XbimEditorCredentials
            {
                EditorsOrganisationName = "INROS LACKNER SE",
                ApplicationFullName = "ILSE XBIM",
                ApplicationDevelopersName = "Bernold Kraft",
                ApplicationVersion = "0.1",
                ApplicationIdentifier = "ILSE XBIM",
                EditorsFamilyName = "Kraft",
                EditorsGivenName = "Bernold"
            };

            var removalRequest = new IfcPropertySetRemovalRequest
            {
                BlackListNames = pSetNames,
                IsLogEnabled = false,
                IsNameMatchingCaseSensitive = false
            };

            List<string> outFileNames = new List<string>();
            foreach (var fileName in fileNames)
            {
                using (var model = IfcStore.Open(fileName))
                {
                    removalRequest.EditorCredentials = editorCredentials;
                    var task = removalRequest.Run(model, null);
                    task.Wait();


                    if (task.Result.ResultCode == Bitub.Ifc.Transform.TransformResult.Code.Finished)
                    {
                        var transformedModel = task.Result.Target;
                        var outFileName = Regex.Replace(
                            Path.GetFileNameWithoutExtension(fileName),
                            replacePattern,
                            replaceWith).Trim();

                        var pathOutFileName = $@"{targetDirectory}\{outFileName}{Path.GetExtension(fileName)}";
                        using (var outputFile = File.Create(pathOutFileName))
                        {
                            transformedModel.SaveAsIfc(outputFile);
                            outputFile.Close();
                        }

                        transformedModel.Dispose();
                        outFileNames.Add(pathOutFileName);
                    }
                }
            }
            return outFileNames.ToArray();
        }

        /// <summary>
        /// Exports an IFC model as binary Scene model.
        /// </summary>
        /// <param name="fileNames"></param>
        /// <returns>Exported model file names</returns>
        public static string[] ExportBinSceneModel(string[] fileNames)
        {
            List<string> outFileNames = new List<string>();
            var exporter = new IfcSceneExporter(new XbimTesselationContext());
            exporter.Settings = new IfcSceneExportSettings
            {
                Positioning = ScenePositioningStrategy.NoCorrection,
                Transforming = SceneTransformationStrategy.Quaternion                
            };
            foreach (var fileName in fileNames)
            {                
                using (var model = IfcStore.Open(fileName))
                {
                    var task = exporter.Run(model);
                    task.Wait();

                    var sceneFileName = $@"{Path.GetDirectoryName(fileName)}\{Path.GetFileNameWithoutExtension(fileName)}.scene";
                    outFileNames.Add(sceneFileName);
                    using (var binStream = File.Create(sceneFileName))
                    {
                        var binScene = task.Result.Scene.ToByteArray();
                        binStream.Write(binScene, 0, binScene.Length);
                    }
                }
            }
            return outFileNames.ToArray();
        }
    }
}
