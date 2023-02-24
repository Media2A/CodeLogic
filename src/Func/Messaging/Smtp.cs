using System;
using System.Net.Mail;
using System.Web;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public bool SendSmtp(string emailFrom, string emailTo, string emailCC, string emailSubject, string emailBody, bool isHtml)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(emailFrom);

                // The important part -- configuring the SMTP client
                SmtpClient smtp = new SmtpClient();
                smtp.Port = 25;   // [1] You can try with 465 also, I always used 587 and got success
                smtp.EnableSsl = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network; // [2] Added this
                smtp.UseDefaultCredentials = false; // [3] Changed this
                smtp.Credentials = new System.Net.NetworkCredential(emailFrom, "password_here");  // [4] Added this. Note, first parameter is NOT string.
                smtp.Host = "smtp.gmail.com";

                //recipient address
                mail.To.Add(new MailAddress(emailTo));

                //Formatted mail body
                mail.IsBodyHtml = isHtml;
                string st = emailBody;
                mail.Subject = emailSubject;
                mail.Body = st;
                smtp.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
                // Error message

                return false;
            }
        }
    }
}