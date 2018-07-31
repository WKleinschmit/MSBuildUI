﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.Profiler;

namespace MSBuildLogging
{
    /// <summary>
    /// Deserializes and returns BuildEventArgs-derived objects from a BinaryReader
    /// </summary>
    internal class BuildEventArgsReader
    {
        private readonly BinaryReader binaryReader;
        private readonly int fileFormatVersion;

        // reflection is needed to set these three fields because public constructors don't provide
        // a way to set these from the outside
        private static readonly FieldInfo buildEventArgsFieldThreadId =
            typeof(BuildEventArgs).GetField("threadId", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo buildEventArgsFieldSenderName =
            typeof(BuildEventArgs).GetField("senderName", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo buildEventArgsFieldTimestamp =
            typeof(BuildEventArgs).GetField("timestamp", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Initializes a new instance of BuildEventArgsReader using a BinaryReader instance
        /// </summary>
        /// <param name="binaryReader">The BinaryReader to read BuildEventArgs from</param>
        /// <param name="fileFormatVersion">The file format version of the log file being read.</param>
        public BuildEventArgsReader(BinaryReader binaryReader, int fileFormatVersion)
        {
            this.binaryReader = binaryReader;
            this.fileFormatVersion = fileFormatVersion;
        }

        /// <summary>
        /// Raised when the log reader encounters a binary blob embedded in the stream.
        /// The arguments include the blob kind and the byte buffer with the contents.
        /// </summary>
        internal event Action<BinaryLogRecordKind, byte[]> OnBlobRead;

        /// <summary>
        /// Reads the next log record from the binary reader. If there are no more records, returns null.
        /// </summary>
        public BuildEventArgs Read()
        {
            BinaryLogRecordKind recordKind = (BinaryLogRecordKind)ReadInt32();

            while (IsBlob(recordKind))
            {
                ReadBlob(recordKind);

                recordKind = (BinaryLogRecordKind)ReadInt32();
            }

            BuildEventArgs result = null;
            switch (recordKind)
            {
                case BinaryLogRecordKind.EndOfFile:
                    break;
                case BinaryLogRecordKind.BuildStarted:
                    result = ReadBuildStartedEventArgs();
                    break;
                case BinaryLogRecordKind.BuildFinished:
                    result = ReadBuildFinishedEventArgs();
                    break;
                case BinaryLogRecordKind.ProjectStarted:
                    result = ReadProjectStartedEventArgs();
                    break;
                case BinaryLogRecordKind.ProjectFinished:
                    result = ReadProjectFinishedEventArgs();
                    break;
                case BinaryLogRecordKind.TargetStarted:
                    result = ReadTargetStartedEventArgs();
                    break;
                case BinaryLogRecordKind.TargetFinished:
                    result = ReadTargetFinishedEventArgs();
                    break;
                case BinaryLogRecordKind.TaskStarted:
                    result = ReadTaskStartedEventArgs();
                    break;
                case BinaryLogRecordKind.TaskFinished:
                    result = ReadTaskFinishedEventArgs();
                    break;
                case BinaryLogRecordKind.Error:
                    result = ReadBuildErrorEventArgs();
                    break;
                case BinaryLogRecordKind.Warning:
                    result = ReadBuildWarningEventArgs();
                    break;
                case BinaryLogRecordKind.Message:
                    result = ReadBuildMessageEventArgs();
                    break;
                case BinaryLogRecordKind.CriticalBuildMessage:
                    result = ReadCriticalBuildMessageEventArgs();
                    break;
                case BinaryLogRecordKind.TaskCommandLine:
                    result = ReadTaskCommandLineEventArgs();
                    break;
                case BinaryLogRecordKind.ProjectEvaluationStarted:
                    result = ReadProjectEvaluationStartedEventArgs();
                    break;
                case BinaryLogRecordKind.ProjectEvaluationFinished:
                    result = ReadProjectEvaluationFinishedEventArgs();
                    break;
                case BinaryLogRecordKind.ProjectImported:
                    result = ReadProjectImportedEventArgs();
                    break;
                case BinaryLogRecordKind.TargetSkipped:
                    result = ReadTargetSkippedEventArgs();
                    break;
            }

            return result;
        }

        /// <summary>
        /// For now it's just the ProjectImportArchive.
        /// </summary>
        private static bool IsBlob(BinaryLogRecordKind recordKind)
        {
            return recordKind == BinaryLogRecordKind.ProjectImportArchive;
        }

        private void ReadBlob(BinaryLogRecordKind kind)
        {
            int length = ReadInt32();
            byte[] bytes = binaryReader.ReadBytes(length);
            OnBlobRead?.Invoke(kind, bytes);
        }

        private BuildEventArgs ReadProjectImportedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            // Read unused Importance, it defaults to Low
            ReadInt32();

            bool importIgnored = false;

            // the ImportIgnored field was introduced in file format version 3
            if (fileFormatVersion > 2)
            {
                importIgnored = ReadBoolean();
            }

            string importedProjectFile = ReadOptionalString();
            string unexpandedProject = ReadOptionalString();

            ProjectImportedEventArgs e = new ProjectImportedEventArgs(
                fields.LineNumber,
                fields.ColumnNumber,
                fields.Message);

            SetCommonFields(e, fields);

            e.ProjectFile = fields.ProjectFile;

            e.ImportedProjectFile = importedProjectFile;
            e.UnexpandedProject = unexpandedProject;
            e.ImportIgnored = importIgnored;
            return e;
        }

        private BuildEventArgs ReadTargetSkippedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            // Read unused Importance, it defaults to Low
            ReadInt32();
            string targetFile = ReadOptionalString();
            string targetName = ReadOptionalString();
            string parentTarget = ReadOptionalString();
            TargetBuiltReason buildReason = (TargetBuiltReason)ReadInt32();

            TargetSkippedEventArgs e = new TargetSkippedEventArgs(
                fields.Message);

            SetCommonFields(e, fields);

            e.ProjectFile = fields.ProjectFile;
            e.TargetFile = targetFile;
            e.TargetName = targetName;
            e.ParentTarget = parentTarget;
            e.BuildReason = buildReason;

            return e;
        }

        private BuildEventArgs ReadBuildStartedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            Dictionary<string, string> environment = ReadStringDictionary();

            BuildStartedEventArgs e = new BuildStartedEventArgs(
                fields.Message,
                fields.HelpKeyword,
                environment);
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadBuildFinishedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            bool succeeded = ReadBoolean();

            BuildFinishedEventArgs e = new BuildFinishedEventArgs(
                fields.Message,
                fields.HelpKeyword,
                succeeded,
                fields.Timestamp);
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadProjectEvaluationStartedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            string projectFile = ReadString();

            ProjectEvaluationStartedEventArgs e = new ProjectEvaluationStartedEventArgs(fields.Message)
            {
                ProjectFile = projectFile
            };
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadProjectEvaluationFinishedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            string projectFile = ReadString();

            ProjectEvaluationFinishedEventArgs e = new ProjectEvaluationFinishedEventArgs(fields.Message)
            {
                ProjectFile = projectFile
            };
            SetCommonFields(e, fields);

            // ProfilerResult was introduced in version 5
            if (fileFormatVersion > 4)
            {
                bool hasProfileData = ReadBoolean();
                if (hasProfileData)
                {
                    int count = ReadInt32();

                    Dictionary<EvaluationLocation, ProfiledLocation> d = new Dictionary<EvaluationLocation, ProfiledLocation>(count);
                    for (int i = 0; i < count; i++)
                    {
                        d.Add(ReadEvaluationLocation(), ReadProfiledLocation());
                    }
                    e.ProfilerResult = new ProfilerResult(d);
                }
            }

            return e;
        }

        private BuildEventArgs ReadProjectStartedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            BuildEventContext parentContext = null;
            if (ReadBoolean())
            {
                parentContext = ReadBuildEventContext();
            }

            string projectFile = ReadOptionalString();
            int projectId = ReadInt32();
            string targetNames = ReadString();
            string toolsVersion = ReadOptionalString();

            Dictionary<string, string> globalProperties = null;

            if (fileFormatVersion > 6)
            {
                if (ReadBoolean())
                {
                    globalProperties = ReadStringDictionary();
                }
            }

            ArrayList propertyList = ReadPropertyList();
            IEnumerable itemList = ReadItems();

            ProjectStartedEventArgs e = new ProjectStartedEventArgs(
                projectId,
                fields.Message,
                fields.HelpKeyword,
                projectFile,
                targetNames,
                propertyList,
                itemList,
                parentContext,
                globalProperties,
                toolsVersion);
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadProjectFinishedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            string projectFile = ReadOptionalString();
            bool succeeded = ReadBoolean();

            ProjectFinishedEventArgs e = new ProjectFinishedEventArgs(
                fields.Message,
                fields.HelpKeyword,
                projectFile,
                succeeded,
                fields.Timestamp);
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadTargetStartedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            string targetName = ReadOptionalString();
            string projectFile = ReadOptionalString();
            string targetFile = ReadOptionalString();
            string parentTarget = ReadOptionalString();
            // BuildReason was introduced in version 4
            TargetBuiltReason buildReason = fileFormatVersion > 3 ? (TargetBuiltReason)ReadInt32() : TargetBuiltReason.None;

            TargetStartedEventArgs e = new TargetStartedEventArgs(
                fields.Message,
                fields.HelpKeyword,
                targetName,
                projectFile,
                targetFile,
                parentTarget,
                buildReason,
                fields.Timestamp);
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadTargetFinishedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            bool succeeded = ReadBoolean();
            string projectFile = ReadOptionalString();
            string targetFile = ReadOptionalString();
            string targetName = ReadOptionalString();
            IEnumerable targetOutputItemList = ReadItemList();

            TargetFinishedEventArgs e = new TargetFinishedEventArgs(
                fields.Message,
                fields.HelpKeyword,
                targetName,
                projectFile,
                targetFile,
                succeeded,
                fields.Timestamp,
                targetOutputItemList);
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadTaskStartedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            string taskName = ReadOptionalString();
            string projectFile = ReadOptionalString();
            string taskFile = ReadOptionalString();

            TaskStartedEventArgs e = new TaskStartedEventArgs(
                fields.Message,
                fields.HelpKeyword,
                projectFile,
                taskFile,
                taskName,
                fields.Timestamp);
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadTaskFinishedEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            bool succeeded = ReadBoolean();
            string taskName = ReadOptionalString();
            string projectFile = ReadOptionalString();
            string taskFile = ReadOptionalString();

            TaskFinishedEventArgs e = new TaskFinishedEventArgs(
                fields.Message,
                fields.HelpKeyword,
                projectFile,
                taskFile,
                taskName,
                succeeded,
                fields.Timestamp);
            SetCommonFields(e, fields);
            return e;
        }

        private BuildEventArgs ReadBuildErrorEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            ReadDiagnosticFields(fields);

            BuildErrorEventArgs e = new BuildErrorEventArgs(
                fields.Subcategory,
                fields.Code,
                fields.File,
                fields.LineNumber,
                fields.ColumnNumber,
                fields.EndLineNumber,
                fields.EndColumnNumber,
                fields.Message,
                fields.HelpKeyword,
                fields.SenderName,
                fields.Timestamp)
            {
                BuildEventContext = fields.BuildEventContext,
                ProjectFile = fields.ProjectFile
            };
            return e;
        }

        private BuildEventArgs ReadBuildWarningEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            ReadDiagnosticFields(fields);

            BuildWarningEventArgs e = new BuildWarningEventArgs(
                fields.Subcategory,
                fields.Code,
                fields.File,
                fields.LineNumber,
                fields.ColumnNumber,
                fields.EndLineNumber,
                fields.EndColumnNumber,
                fields.Message,
                fields.HelpKeyword,
                fields.SenderName,
                fields.Timestamp)
            {
                BuildEventContext = fields.BuildEventContext,
                ProjectFile = fields.ProjectFile
            };
            return e;
        }

        private BuildEventArgs ReadBuildMessageEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            MessageImportance importance = (MessageImportance)ReadInt32();

            BuildMessageEventArgs e = new BuildMessageEventArgs(
                fields.Subcategory,
                fields.Code,
                fields.File,
                fields.LineNumber,
                fields.ColumnNumber,
                fields.EndLineNumber,
                fields.EndColumnNumber,
                fields.Message,
                fields.HelpKeyword,
                fields.SenderName,
                importance,
                fields.Timestamp)
            {
                BuildEventContext = fields.BuildEventContext,
                ProjectFile = fields.ProjectFile
            };
            return e;
        }

        private BuildEventArgs ReadTaskCommandLineEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            MessageImportance importance = (MessageImportance)ReadInt32();
            string commandLine = ReadOptionalString();
            string taskName = ReadOptionalString();

            TaskCommandLineEventArgs e = new TaskCommandLineEventArgs(
                commandLine,
                taskName,
                importance,
                fields.Timestamp)
            {
                BuildEventContext = fields.BuildEventContext,
                ProjectFile = fields.ProjectFile
            };
            return e;
        }

        private BuildEventArgs ReadCriticalBuildMessageEventArgs()
        {
            BuildEventArgsFields fields = ReadBuildEventArgsFields();
            /*(MessageImportance)*/ReadInt32();

            CriticalBuildMessageEventArgs e = new CriticalBuildMessageEventArgs(
                fields.Subcategory,
                fields.Code,
                fields.File,
                fields.LineNumber,
                fields.ColumnNumber,
                fields.EndLineNumber,
                fields.EndColumnNumber,
                fields.Message,
                fields.HelpKeyword,
                fields.SenderName,
                fields.Timestamp)
            {
                BuildEventContext = fields.BuildEventContext,
                ProjectFile = fields.ProjectFile
            };
            return e;
        }

        /// <summary>
        /// For errors and warnings these 8 fields are written out explicitly
        /// (their presence is not marked as a bit in the flags). So we have to
        /// read explicitly.
        /// </summary>
        /// <param name="fields"></param>
        private void ReadDiagnosticFields(BuildEventArgsFields fields)
        {
            fields.Subcategory = ReadOptionalString();
            fields.Code = ReadOptionalString();
            fields.File = ReadOptionalString();
            fields.ProjectFile = ReadOptionalString();
            fields.LineNumber = ReadInt32();
            fields.ColumnNumber = ReadInt32();
            fields.EndLineNumber = ReadInt32();
            fields.EndColumnNumber = ReadInt32();
        }

        private BuildEventArgsFields ReadBuildEventArgsFields()
        {
            BuildEventArgsFieldFlags flags = (BuildEventArgsFieldFlags)ReadInt32();
            BuildEventArgsFields result = new BuildEventArgsFields
            {
                Flags = flags
            };

            if ((flags & BuildEventArgsFieldFlags.Message) != 0)
            {
                result.Message = ReadString();
            }

            if ((flags & BuildEventArgsFieldFlags.BuildEventContext) != 0)
            {
                result.BuildEventContext = ReadBuildEventContext();
            }

            if ((flags & BuildEventArgsFieldFlags.ThreadId) != 0)
            {
                result.ThreadId = ReadInt32();
            }

            if ((flags & BuildEventArgsFieldFlags.HelpHeyword) != 0)
            {
                result.HelpKeyword = ReadString();
            }

            if ((flags & BuildEventArgsFieldFlags.SenderName) != 0)
            {
                result.SenderName = ReadString();
            }

            if ((flags & BuildEventArgsFieldFlags.Timestamp) != 0)
            {
                result.Timestamp = ReadDateTime();
            }

            if ((flags & BuildEventArgsFieldFlags.Subcategory) != 0)
            {
                result.Subcategory = ReadString();
            }

            if ((flags & BuildEventArgsFieldFlags.Code) != 0)
            {
                result.Code = ReadString();
            }

            if ((flags & BuildEventArgsFieldFlags.File) != 0)
            {
                result.File = ReadString();
            }

            if ((flags & BuildEventArgsFieldFlags.ProjectFile) != 0)
            {
                result.ProjectFile = ReadString();
            }

            if ((flags & BuildEventArgsFieldFlags.LineNumber) != 0)
            {
                result.LineNumber = ReadInt32();
            }

            if ((flags & BuildEventArgsFieldFlags.ColumnNumber) != 0)
            {
                result.ColumnNumber = ReadInt32();
            }

            if ((flags & BuildEventArgsFieldFlags.EndLineNumber) != 0)
            {
                result.EndLineNumber = ReadInt32();
            }

            if ((flags & BuildEventArgsFieldFlags.EndColumnNumber) != 0)
            {
                result.EndColumnNumber = ReadInt32();
            }

            return result;
        }

        private void SetCommonFields(BuildEventArgs buildEventArgs, BuildEventArgsFields fields)
        {
            buildEventArgs.BuildEventContext = fields.BuildEventContext;

            if ((fields.Flags & BuildEventArgsFieldFlags.ThreadId) != 0)
            {
                buildEventArgsFieldThreadId.SetValue(buildEventArgs, fields.ThreadId);
            }

            if ((fields.Flags & BuildEventArgsFieldFlags.SenderName) != 0)
            {
                buildEventArgsFieldSenderName.SetValue(buildEventArgs, fields.SenderName);
            }

            if ((fields.Flags & BuildEventArgsFieldFlags.Timestamp) != 0)
            {
                buildEventArgsFieldTimestamp.SetValue(buildEventArgs, fields.Timestamp);
            }
        }

        private ArrayList ReadPropertyList()
        {
            Dictionary<string, string> properties = ReadStringDictionary();
            if (properties == null)
            {
                return null;
            }

            ArrayList list = new ArrayList();
            foreach (KeyValuePair<string, string> property in properties)
            {
                DictionaryEntry entry = new DictionaryEntry(property.Key, property.Value);
                list.Add(entry);
            }

            return list;
        }

        private BuildEventContext ReadBuildEventContext()
        {
            int nodeId = ReadInt32();
            int projectContextId = ReadInt32();
            int targetId = ReadInt32();
            int taskId = ReadInt32();
            int submissionId = ReadInt32();
            int projectInstanceId = ReadInt32();

            // evaluationId was introduced in format version 2
            int evaluationId = BuildEventContext.InvalidEvaluationId;
            if (fileFormatVersion > 1)
            {
                evaluationId = ReadInt32();
            }

            BuildEventContext result = new BuildEventContext(
                submissionId,
                nodeId,
                evaluationId,
                projectInstanceId,
                projectContextId,
                targetId,
                taskId);
            return result;
        }

        private Dictionary<string, string> ReadStringDictionary()
        {
            int count = ReadInt32();

            if (count == 0)
            {
                return null;
            }

            Dictionary<string, string> result = new Dictionary<string, string>(count);
            for (int i = 0; i < count; i++)
            {
                string key = ReadString();
                string value = ReadString();
                result[key] = value;
            }

            return result;
        }

        private class TaskItem : ITaskItem
        {
            public string ItemSpec { get; set; }
            public Dictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

            public int MetadataCount => Metadata.Count;

            public ICollection MetadataNames => Metadata.Keys;

            public IDictionary CloneCustomMetadata()
            {
                return Metadata;
            }

            public void CopyMetadataTo(ITaskItem destinationItem)
            {
                throw new NotImplementedException();
            }

            public string GetMetadata(string metadataName)
            {
                return Metadata[metadataName];
            }

            public void RemoveMetadata(string metadataName)
            {
                throw new NotImplementedException();
            }

            public void SetMetadata(string metadataName, string metadataValue)
            {
                throw new NotImplementedException();
            }
        }

        private ITaskItem ReadItem()
        {
            TaskItem item = new TaskItem
            {
                ItemSpec = ReadString()
            };

            int count = ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string name = ReadString();
                string value = ReadString();
                item.Metadata[name] = value;
            }

            return item;
        }

        private IEnumerable ReadItems()
        {
            int count = ReadInt32();
            if (count == 0)
            {
                return null;
            }

            List<DictionaryEntry> list = new List<DictionaryEntry>(count);

            for (int i = 0; i < count; i++)
            {
                string key = ReadString();
                ITaskItem item = ReadItem();
                list.Add(new DictionaryEntry(key, item));
            }

            return list;
        }

        private IEnumerable ReadItemList()
        {
            int count = ReadInt32();
            if (count == 0)
            {
                return null;
            }

            List<ITaskItem> list = new List<ITaskItem>(count);

            for (int i = 0; i < count; i++)
            {
                ITaskItem item = ReadItem();
                list.Add(item);
            }

            return list;
        }

        private string ReadOptionalString()
        {
            if (ReadBoolean())
            {
                return ReadString();
            }
            else
            {
                return null;
            }
        }

        private string ReadString()
        {
            return binaryReader.ReadString();
        }

        private int ReadInt32()
        {
            return Read7BitEncodedInt(binaryReader);
        }

        private long ReadInt64()
        {
            return binaryReader.ReadInt64();
        }

        private bool ReadBoolean()
        {
            return binaryReader.ReadBoolean();
        }

        private DateTime ReadDateTime()
        {
            return new DateTime(binaryReader.ReadInt64(), (DateTimeKind)ReadInt32());
        }

        private TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(binaryReader.ReadInt64());
        }

        private int Read7BitEncodedInt(BinaryReader reader)
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                {
                    throw new FormatException();
                }

                // ReadByte handles end of stream cases for us.
                b = reader.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        private ProfiledLocation ReadProfiledLocation()
        {
            int numberOfHits = ReadInt32();
            TimeSpan exclusiveTime = ReadTimeSpan();
            TimeSpan inclusiveTime = ReadTimeSpan();

            return new ProfiledLocation(inclusiveTime, exclusiveTime, numberOfHits);
        }

        private EvaluationLocation ReadEvaluationLocation()
        {
            string elementName = ReadOptionalString();
            string description = ReadOptionalString();
            string evaluationDescription = ReadOptionalString();
            string file = ReadOptionalString();
            EvaluationLocationKind kind = (EvaluationLocationKind)ReadInt32();
            EvaluationPass evaluationPass = (EvaluationPass)ReadInt32();

            int? line = null;
            bool hasLine = ReadBoolean();
            if (hasLine)
            {
                line = ReadInt32();
            }

            // Id and parent Id were introduced in version 6
            if (fileFormatVersion > 5)
            {
                long id = ReadInt64();
                long? parentId = null;
                bool hasParent = ReadBoolean();
                if (hasParent)
                {
                    parentId = ReadInt64();

                }
                return new EvaluationLocation(id, parentId, evaluationPass, evaluationDescription, file, line, elementName, description, kind);
            }

            return new EvaluationLocation(0, null, evaluationPass, evaluationDescription, file, line, elementName, description, kind);
        }
    }
}
