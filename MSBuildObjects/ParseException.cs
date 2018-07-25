using System;
using System.Runtime.Serialization;

namespace MSBuildObjects
{
    public class ParseException : ApplicationException
    {
        public ParseException()
        { }

        public ParseException(string message)
            : base(message) { }

        protected ParseException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public ParseException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
