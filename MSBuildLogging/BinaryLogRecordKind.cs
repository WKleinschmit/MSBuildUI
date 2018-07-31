using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBuildLogging
{
    internal enum BinaryLogRecordKind
    {
        EndOfFile = 0,
        BuildStarted,
        BuildFinished,
        ProjectStarted,
        ProjectFinished,
        TargetStarted,
        TargetFinished,
        TaskStarted,
        TaskFinished,
        Error,
        Warning,
        Message,
        TaskCommandLine,
        CriticalBuildMessage,
        ProjectEvaluationStarted,
        ProjectEvaluationFinished,
        ProjectImported,
        ProjectImportArchive,
        TargetSkipped
    }
}
