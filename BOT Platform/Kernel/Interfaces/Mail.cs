using System;
using System.Net.Mail;
using System.Windows.Forms;

namespace BOT_Platform.Kernel.Interfaces
{
    class Mail
    {
        /// <summary>
        /// Отправляет письмо пользователя на почту поддержки
        /// </summary>
        /// <param name="smtpServer">Сервер исходящей почты</param>
        /// <param name="from">(по умолчанию support-team.soft@yandex.ru)</param>
        /// <param name="password">(по умолчанию - пароль почты support-team)</param>
        /// <param name="mailto">Почта адресата</param>
        /// <param name="caption">Тема</param>
        /// <param name="message">Текст письма</param>

        public static void SendMail(string smtpServer, string from, string password,
        string mailto, string caption, string message)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(smtpServer);

                mail.From = new MailAddress(from);
                mail.To.Add(mailto);
                mail.Subject = caption;
                mail.Body = message;

                SmtpServer.UseDefaultCredentials = false;
                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential(from, password);
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                MessageBox.Show("Письмо было успешно отправлено", "Доставлено");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }
    }
}