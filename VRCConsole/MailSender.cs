using System;
using System.Net.Mail;

namespace vrc
{
    
    /// <summary>
    /// Send mail to Administrator using SMTP 
    /// </summary>
    class MailSender
    {
        //variable used for sychronisation
        private Object syn = new Object(); 

        // default "From" and "Subject" fields
        private static String mFrom = "report@vrdcontroller";
        private static String mSubject = "VRC Error";

        private static MailSender mailSenderInstance = null;

        private static Boolean fMailSendImpossible = false; 
 
		/// <summary>
		/// Private constructor to prevent instantiation
		/// </summary>
		private MailSender() {
 
		}
 
		/// <summary>
		/// Static constructor that gets
		/// called only once during the application's lifetime.
		/// </summary>
        static MailSender()
        {
            mailSenderInstance = new MailSender();
		}


        /// <summary>
        /// Static property to retrieve the instance of the MailSender
        /// </summary>
        public static MailSender Instance
        {
            get
            {
                if (mailSenderInstance != null)
                {
                    return mailSenderInstance;
                }
                return null;
            }
        }


      
        /// <summary>
        /// Send an Email with subject and message (sychronised method)
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        public void send(String subject, String message)
        {
            if (fMailSendImpossible)
            {
                return;
            }

            lock (syn)
                {
                String smtpServer = Configuration.smtpServer;
                int smtpServerPort = Configuration.smtpServerPort;

                String receipt = Configuration.mailRecipient;


                try
                {
                    MailMessage mailMsg = new MailMessage();
                    mailMsg.To.Add(receipt);

                    // From
                    MailAddress mailAddress = new MailAddress(mFrom);
                    mailMsg.From = mailAddress;

                    // Subject and Body
                    mailMsg.Subject = subject;
                    mailMsg.Body = message;

                    // Init SmtpClient and send
                    SmtpClient smtpClient = new SmtpClient(smtpServer, smtpServerPort);
                    smtpClient.Send(mailMsg);
                }
                catch (Exception ex)
                {
                    fMailSendImpossible = true;
                    LogWriter.error(ex.Message);
                }
              }
        }

        /// <summary>
        /// Send an Email with the given message (use the default subject) 
        /// </summary>
        /// <param name="message">message content</param>
        public void send(String message)
        {
            this.send(mSubject, message);
        }


        public void send(string format, params object[] args)
        {
            string pText = String.Format(format, args);
            send(pText);
        }

    }
}
