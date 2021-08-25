using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

using Autodesk.DesignScript.Runtime;

using Bitub.Dto;

using TRex.Log;
using TRex.Internal;

namespace TRex.Store
{
    /// <summary>
    /// Model base implementation using a qualifier as naming approach.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public abstract class ProgressingModelTask<TModel> : ProgressingTask
    {
#pragma warning disable CS1591

        #region Internals

        protected internal Logger Logger { get; private set; }

        protected internal Qualifier Qualifier { get; private set; }

        protected internal ProgressingModelTask(Qualifier qualifier, Logger logger, LogMessage[] propagateLog = null)
        {
            Logger = logger;
            Qualifier = qualifier;
            
            if (null != propagateLog)
                ActionLog = new System.Collections.ObjectModel.ObservableCollection<LogMessage>(propagateLog);

            ActionLog.CollectionChanged += ActionLog_CollectionChanged;
        }

        protected abstract TModel RequalifyModel(Qualifier qualifier);

        /// <summary>
        /// Forwards action log changes to logging instance.
        /// </summary>
        /// <param name="sender">A sender</param>
        /// <param name="e">The event args</param>
        protected void ActionLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (LogMessage msg in e.NewItems.Cast<LogMessage>())
                {
                    switch (msg.Severity)
                    {
                        case LogSeverity.Debug:
                        case LogSeverity.Info:
                            Logger?.LogInfo(msg.ToString());
                            break;
                        case LogSeverity.Warning:
                            Logger?.LogWarning(msg.ToString());
                            break;
                        case LogSeverity.Critical:
                        case LogSeverity.Error:
                            Logger?.LogError(msg.ToString());
                            break;
                    }
                }
            }
        }

        #endregion

        #region Pure internal helpers

        internal string GetFilePathName(string canonicalSep = "-", bool withExtension = true)
        {
            return GetFilePathName(Qualifier, canonicalSep, withExtension);
        }

        internal static string GetFilePathName(Qualifier qualifier, string canonicalSep = "-", bool withExtension = true)
        {
            switch (qualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    var fileName1 = $"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{qualifier.Anonymous.ToBase64String()}";
                    return fileName1;
                case Qualifier.GuidOrNameOneofCase.Named:
                    var fileName2 = string.IsNullOrEmpty(canonicalSep) ? qualifier.Named.Frags[1] : qualifier.Named.ToLabel(canonicalSep, 1, 1);
                    var fullFileName2 = $"{qualifier.Named.Frags[0]}{Path.DirectorySeparatorChar}{fileName2}";
                    return withExtension ? $"{fullFileName2}.{qualifier.Named.Frags.Last()}" : fullFileName2;
                default:
                    throw new NotSupportedException($"Missing qualifier");
            }
        }

        internal static Qualifier BuildQualifier(params string[] pathNameFileNameAndExtension)
        {
            var name = new Name();
            name.Frags.AddRange(pathNameFileNameAndExtension);
            return new Qualifier
            {
                Named = name
            };
        }

        internal static Qualifier BuildQualifierByFilePathName(string filePathName)
        {
            return BuildQualifier(
                Path.GetDirectoryName(filePathName),
                Path.GetFileNameWithoutExtension(filePathName),
                Path.GetExtension(filePathName).Substring(1));
        }

        internal static Qualifier BuildCanonicalQualifier(Qualifier sourceQualifier, string canonicalName)
        {
            if (null == sourceQualifier)
                return BuildQualifierByFilePathName(canonicalName);

            Qualifier newQualifier;
            switch (sourceQualifier.GuidOrNameCase)
            {
                case Qualifier.GuidOrNameOneofCase.Anonymous:
                    throw new ArgumentException($"Source is temporary model qualifier");
                case Qualifier.GuidOrNameOneofCase.None:
                    newQualifier = BuildQualifierByFilePathName(canonicalName);
                    break;
                case Qualifier.GuidOrNameOneofCase.Named:
                    newQualifier = new Qualifier(sourceQualifier);
                    newQualifier.Named.Frags.Insert(newQualifier.Named.Frags.Count - 1, canonicalName);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return newQualifier;
        }

        internal static Qualifier BuildQualifierByFilePathName(string filePathName, string newExtension)
        {
            return BuildQualifier(Path.GetDirectoryName(filePathName), Path.GetFileNameWithoutExtension(filePathName), newExtension);
        }

        internal static Qualifier BuildQualifierByExtension(Qualifier existing, string newExtension)
        {
            var qualifier = new Qualifier(existing);
            if (qualifier.GuidOrNameCase == Qualifier.GuidOrNameOneofCase.Anonymous)
                throw new NotSupportedException();

            qualifier.Named.Frags[qualifier.Named.Frags.Count - 1] = newExtension;
            return qualifier;
        }

#pragma warning restore CS1591

        #endregion

        /// <summary>
        /// The assembled file name.
        /// </summary>
        public virtual string FileName
        {
            get {
                switch (Qualifier.GuidOrNameCase)
                {
                    case Qualifier.GuidOrNameOneofCase.Anonymous:
                        return Qualifier.Anonymous.ToBase64String();
                    case Qualifier.GuidOrNameOneofCase.Named:
                        return $"{Qualifier.Named.Frags[1]}.{Qualifier.Named.Frags.Last()}";
                    default:
                        throw new NotSupportedException($"Missing qualifier");
                }
            }
        }

        /// <summary>
        /// The resource path name of physical store.
        /// </summary>
        public virtual string PathName
        {
            get {
                switch (Qualifier.GuidOrNameCase)
                {
                    case Qualifier.GuidOrNameOneofCase.Anonymous:
                        return Path.GetTempPath();
                    case Qualifier.GuidOrNameOneofCase.Named:
                        return Qualifier.Named.Frags[0];
                    default:
                        throw new NotSupportedException($"Missing qualifier");
                }
            }
        }

        /// <summary>
        /// The format extension ("ifc", "ifcxml" or "ifczip")
        /// </summary>
        public virtual string FormatExtension
        {
            get {
                switch (Qualifier.GuidOrNameCase)
                {
                    case Qualifier.GuidOrNameOneofCase.Anonymous:
                        return null;
                    case Qualifier.GuidOrNameOneofCase.Named:
                        return Qualifier.Named.Frags.Last();
                    default:
                        throw new NotSupportedException($"Missing qualifier");
                }
            }
        }

        /// <summary>
        /// True, if only a temporary (non-physically based) model.
        /// </summary>
        protected internal bool IsTemporaryModel
        {
            get => Qualifier.GuidOrNameCase != Qualifier.GuidOrNameOneofCase.Named;
        }

        /// <summary>
        /// The source store name.
        /// </summary>        
        internal override string Name
        {
            get {
                switch (Qualifier.GuidOrNameCase)
                {
                    case Qualifier.GuidOrNameOneofCase.Anonymous:
                        return Qualifier.Anonymous.ToBase64String();
                    case Qualifier.GuidOrNameOneofCase.Named:
                        return Qualifier.Named.Frags[1];
                    default:
                        throw new NotSupportedException($"Missing qualifier");
                }
            }
        }

        /// <summary>
        /// Gets the canoncial model file name (depending on preceeding transformations).
        /// </summary>
        /// <param name="seperator">The separator between name fragments</param>
        public virtual string CanonicalFileName(string seperator = "-") => Path.GetFileName(GetFilePathName(seperator, true));

        /// <summary>
        /// Gets the canoncial model name (depending on preceeding transformations).
        /// </summary>
        /// <param name="seperator">The separator between name fragments</param>
        public virtual string CanonicalName(string seperator = "-") => Path.GetFileName(GetFilePathName(seperator, false));

        /// <summary>
        /// Relocates the containing folder.
        /// </summary>
        /// <param name="newPathName">The new path/folder name</param>
        /// <returns>A new reference to the model</returns>
        public virtual TModel RelocatePath(string newPathName)
        {
            Qualifier qualifier;
            if (IsTemporaryModel)
            {
                Logger?.LogInfo($"Relocating temporary model '{Qualifier.Anonymous.ToBase64String()}'");
                qualifier = BuildQualifier(newPathName, Qualifier.Anonymous.ToBase64String());
            }
            else
            {
                qualifier = new Qualifier(Qualifier);
            }

            qualifier.Named.Frags[0] = Path.GetDirectoryName($"{newPathName}{Path.DirectorySeparatorChar}");

            return RequalifyModel(qualifier);
        }

        /// <summary>
        /// Renames the current model's physical name. Does not rename the original
        /// physical resource. The change will only have effect when saving this model to store.
        /// <para>Renaming temporary model will locate the new model relatively to the user's home directory.</para>
        /// </summary>
        /// <param name="fileNameWithoutExt">The new file name without format extension</param>
        public virtual TModel Rename(string fileNameWithoutExt)
        {
            Qualifier qualifier;
            if (IsTemporaryModel)
            {
                Logger?.LogInfo($"Renaming temporary model '{Qualifier.Anonymous.ToBase64String()}'");
                qualifier = BuildQualifier("~", fileNameWithoutExt);
            }
            else
            {
                qualifier = new Qualifier(Qualifier);
            }

            qualifier.Named.Frags[1] = fileNameWithoutExt;
            return RequalifyModel(qualifier);
        }

        /// <summary>
        /// Replaces fragments of the file name (without extension) by given fragments.
        /// <para>Temporary models cannot be renamed by pattern since they don't have a meaningful name.</para>
        /// </summary>
        /// <param name="replacePattern">Regular expression identifiying the replacement</param>
        /// <param name="replaceWith">Fragments to insert</param>
        public virtual TModel RenameWithReplacePattern(string replacePattern, string replaceWith)
        {
            if (IsTemporaryModel)
                throw new NotSupportedException("Not allowed for temporary models");

            var modifiedName = Regex.Replace(Qualifier.Named.Frags[1], replacePattern, replaceWith).Trim();
            return Rename(modifiedName);
        }

        /// <summary>
        /// Renames the current model's physical name append the given canonical suffix. The suffix
        /// is put between name and format extension identifier.
        /// <para>A temporary model cannot have canonical extensions (suffixes)</para>
        /// </summary>
        /// <param name="fragment">A fragment in last position (suffix)</param>
        public virtual TModel RenameWithSuffix(string fragment)
        {
            if (IsTemporaryModel)
                throw new NotSupportedException("Not allowed for temporary models");

            var qualifier = new Qualifier(Qualifier);
            qualifier.Named.Frags.Insert(qualifier.Named.Frags.Count - 1, fragment);
            return RequalifyModel(qualifier);
        }
    }
}