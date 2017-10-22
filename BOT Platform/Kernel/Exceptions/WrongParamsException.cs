using System;

namespace MyFunctions.Exceptions
{
    [Serializable]
    public class BotPlatformException : ApplicationException
    {
        public BotPlatformException() { }
        public BotPlatformException(string message) : base(message) { }
        public BotPlatformException(string message, Exception inner) : base(message, inner) { }
        protected BotPlatformException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WrongParamsException : BotPlatformException
    {
        public WrongParamsException() { }
        public WrongParamsException(string message) : base(message) { }
        public WrongParamsException(string message, Exception inner) : base(message, inner) { }
        protected WrongParamsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}