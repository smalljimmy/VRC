using System;
using System.Collections.Generic;
using System.Text;

namespace vrc
{
    /// <summary>
   /// Exception : Retry times exceed threshold by sending command to LDC-Server
    /// </summary>
   public class TooManyRetryTimesException : Exception
   {
       String description = "Retry times exceed threshold by sending command to LDC-Server";
       override public String ToString() { return description; }
       public TooManyRetryTimesException() { }
   }

   public class NoTransactionException : Exception
   {
       String description = "There's no transaction at the moment";
       override public String ToString() { return description; }
       public NoTransactionException() { }
   }

   public class ReceiveDenyException : Exception
   {
       String description = "The request is denied by LDC-Server";
       override public String ToString() { return description; }
       public ReceiveDenyException() { }
   }

   public class VRCApplicationException : Exception
   {
       public VRCApplicationException(string message) :base(message)
       {
       }
   }

}
