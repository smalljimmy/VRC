using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Security;

namespace vrc
{

    /// <summary>
    /// class for writing log info into file
    /// support different the log level 
    /// uses: Configuration, for log file's location
    /// </summary>
    public class LogWriter
    {
        private static Object sync = new Object(); // used for synchronisation
        private enum _LEVEL { _OFF, _ERROR, _WARNING, _INFO, _DEBUG, _ALL };
        private static _LEVEL logLevel = _LEVEL._OFF;

        private static StreamWriter sw = null; // as buffer for writting logs
        private static Boolean fLogEventImpossible = false;
        private static Boolean fAppendFileImpossible = false;

        private static Boolean bBuffered = false; // whether to buffer log output

        /// <summary>
        /// Thie method register the message into event logger 
        /// The event logger can be access in Windows from remote
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="evenType">the event entry type (error, warning, information)</param>
        public static void logAsEvent(String message, EventLogEntryType eventType){
            
            // if expception happens before by logging event, then just return 
            if (fLogEventImpossible){
                return;
            }


            try
            {
                // Create the event source, if it does not already exist.
                if (!EventLog.SourceExists("Voice Recording Controller"))
                {
                    EventLog.CreateEventSource("Voice Recording Controller", "VRC Log");
                    return;
                }

                // Create an EventLog instance and assign its source.
                EventLog myLog = new EventLog();
                myLog.Source = "Voice Recording Controller";

                // Write an informational entry to the event log.    
                myLog.WriteEntry(message, eventType);
            }
            catch (Exception e)
            {
                // mark event logging as not possible for this run
                fLogEventImpossible = true;

                // event logger needs administrative right
                Console.WriteLine("Can't write into event logger. Details: " + e);
            }

        }


        /// <summary>
        /// Method to write the message into the log file depending on the specific debug level
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="level">the debug level</param>
        private static void append(String message, _LEVEL level)
        {
            if (fAppendFileImpossible)
            {
                return;
            }

            lock (sync)
            {
                // read default logLevel from configuration (lazy loading)
                if (logLevel == _LEVEL._OFF)
                {
                    //load log level from configuration
                    try
                    {
                        logLevel = (_LEVEL)Enum.Parse(typeof(_LEVEL), Configuration.logLevel);
                    }
                    catch
                    {
                        logLevel = _LEVEL._OFF;
                    }
                }



                if (logLevel >= level)
                {
                    DateTime dt = DateTime.Now;

                    try
                    {


                        String filePath = Configuration.logDirectory +"\\" + dt.ToString("yyyyMMdd") + ".log";
                        
                        if (sw == null)
                        {
                            // initialize Streamwriter for the first time
                            sw = File.AppendText(filePath);
                        }

                        // the date changes after last log
                        if (!File.Exists(filePath))
                        {

                            // renew Streamwriter

                            // flush contents of last log file (old date)
                            sw.Flush();
                            sw.Close();

                            // initialize streamwriter for the next log file (new date)
                            sw = File.AppendText(filePath);

                       }

                        

                        // output each substring after '\n' in a new line
                        String[] lines = message.Split(new char[]{'\n'});

                        sw.WriteLine(dt.ToString("hh:mm:ss") + "|" + lines[0]);
                        for ( int i = 1; i < lines.Length; i++ ) 
                        {

                            sw.WriteLine(lines[i]);
                        }

                        if (!bBuffered)
                        {
                            sw.Flush();
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message.ToString());
                        fAppendFileImpossible = true;
                        return;
                    }
                }
            } // end lock
        }



        /// <summary>
        /// log information line (console, logfile, eventlogger)
        /// </summary>
        /// <param name="pText">string to log</param>
        public static void info(String pText)
        {
            Console.WriteLine("<{0}>" , DateTime.Now.ToString());
            Console.WriteLine(pText);

            append(pText, _LEVEL._INFO);
            logAsEvent(pText, EventLogEntryType.Information);
        }

        public static void info(string format, params object[] args)
        {
            string pText = String.Format(format, args);
            info(pText);
        }

        /// <summary>
        /// log error message  (console, logfile, eventlogger)
        /// </summary>
        /// <param name="pText">string to log</param>
        public static void error(String pText)
        {
            lock (sync)
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("<{0}>", DateTime.Now.ToString());
                Console.WriteLine(pText);
                Console.ForegroundColor = oldColor;

            }


            append(pText, _LEVEL._ERROR);
            logAsEvent(pText, EventLogEntryType.Error);
        }


        /// <summary>
        /// log warning message  (console, logfile, eventlogger)
        /// </summary>
        /// <param name="pText">string to log</param>
        public static void warn(String pText)
        {
            lock (sync)
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("<{0}>", DateTime.Now.ToString());
                Console.WriteLine(pText);
                Console.ForegroundColor = oldColor;

            }


            append(pText, _LEVEL._WARNING);
            logAsEvent(pText, EventLogEntryType.Warning);
        }

        public static void warn(string format, params object[] args)
        {
            string pText = String.Format(format, args);
            warn(pText);
        }


        public static void error(string format,  params object[] args)
        {
            string pText = String.Format(format,args);
            error(pText);
        }


        /// <summary>
        /// log warning message (console, logfile)
        /// </summary>
        /// <param name="pText">string to log</param>
        public static void debug(String pText)
        {
            Console.WriteLine("<{0}>" , DateTime.Now.ToString());
            Console.WriteLine(pText);

            append(pText, _LEVEL._DEBUG);
        }

        public static void debug(string format, params object[] args)
        {
            string pText = String.Format(format, args);
            debug(pText);
        }
    }
}
