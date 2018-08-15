using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using Microsoft.Build.Framework;

namespace MSBuildLogging
{
    public class RemoteLogger : ILogger
    {
        internal const int FileFormatVersion = 7;

        NamedPipeClientStream pipeSstream;
        private Stream stream;
        private BinaryWriter binaryWriter;
        private BuildEventArgsWriter eventArgsWriter;
        private string pipeName;

        public RemoteLogger()
        {
        }

        public void Initialize(IEventSource eventSource)
        {
            ProcessParameters();

            try
            {
                pipeSstream = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                pipeSstream.Connect();
            }
            catch (Exception e)
            {
                throw new LoggerException(e.ToString());
            }

            stream = new GZipStream(pipeSstream, CompressionLevel.Optimal);
            binaryWriter = new BinaryWriter(stream);
            eventArgsWriter = new BuildEventArgsWriter(binaryWriter);

            binaryWriter.Write(FileFormatVersion);

            eventSource.AnyEventRaised += EventSource_AnyEventRaised;
        }

        public void Shutdown()
        {
            if (stream == null)
                return;

            // It's hard to determine whether we're at the end of decoding GZipStream
            // so add an explicit 0 at the end to signify end of file
            stream.WriteByte((byte)BinaryLogRecordKind.EndOfFile);
            stream.Flush();
        }

        public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Diagnostic;
        public string Parameters { get; set; }

        private void EventSource_AnyEventRaised(object sender, BuildEventArgs e)
        {
            Write(e);
        }

        private void Write(BuildEventArgs e)
        {
            if (stream == null) return;

            lock (eventArgsWriter)
            {
                eventArgsWriter.Write(e);
            }
        }

        private void ProcessParameters()
        {
            if (Parameters == null)
                throw new LoggerException("MissingBinaryLoggerParameters");

            string[] parameters = Parameters.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string parameter in parameters)
            {
                if (parameter.StartsWith("PipeName=", StringComparison.OrdinalIgnoreCase))
                {
                    pipeName = parameter.Substring(9);
                    continue;
                }

                throw new LoggerException($"InvalidBinaryLoggerParameter: '{parameter}'");
            }
        }
    }
}
