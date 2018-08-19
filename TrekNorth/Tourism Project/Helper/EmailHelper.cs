using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace Tourism_Project.Helper
{
    public static class EmailHelper
    {
        public static bool SendEmail(string From, string To, string Subject, string Body, Stream file, string FileName, bool IsBodyHtml)
        {
            bool isSuccess = false;
            using (MailMessage mm = new MailMessage(From, To))
            {
                mm.Subject = Subject;
                mm.Body = Body;
                if (file != null)
                {
                    mm.Attachments.Add(new Attachment(file, FileName));
                }
                mm.IsBodyHtml = IsBodyHtml;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.EnableSsl = true;
                NetworkCredential NetworkCred = new NetworkCredential("treknorth.com.au@gmail.com", "Sumit$Dhara26");
                smtp.UseDefaultCredentials = true;
                smtp.Credentials = NetworkCred;
                smtp.Port = 587;
                try
                {
                    smtp.Send(mm);
                    isSuccess = true;
                }
                catch(Exception ex) 
                {
                    isSuccess = false;
                }

                return isSuccess;
            }
        }
    }
}