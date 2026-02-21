using System;

namespace At.Matus.IO.NmmReader
{
    [Serializable()]
    public class NmmFileException : Exception
    {
        public NmmFileException() : base() { }
        public NmmFileException(string message) : base(message) { }
        public NmmFileException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected NmmFileException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
