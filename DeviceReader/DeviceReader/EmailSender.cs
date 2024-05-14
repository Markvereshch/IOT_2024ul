using Azure;
using Azure.Communication.Email;

namespace AgentApp
{
    internal static class EmailSender
    {
        static readonly EmailClient? emailClient;
        static readonly string? sender;
        static readonly List<EmailAddress> recipients;
        static EmailSender()
        {
            AppSettings settings = AppSettings.GetSettings();
            sender = settings.CommunicationServicesSender;
            recipients = new List<EmailAddress>();
            foreach(string email in settings.EmailAddresses)
            {
                recipients.Add(new EmailAddress(email));
            }
            var connectionString = settings.CommunicationServicesConnectionString;
            try
            {
                emailClient = new EmailClient(connectionString);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"{DateTime.Now}: EMAIL CLIENT ERROR! Invalid connection string to Azure Communication Services. Please verify your connection string.");
            }
        }
        static public async Task SendErrorMessageToEmailsAsync(string message)
        {
            if (recipients.Count == 0)
            {
                throw new InvalidDataException("There are no addresses in the configuration file");
            }
            Console.WriteLine($"{DateTime.Now}: Sending message to {recipients.Count} email address/addresses...");

            string body = $"An error has occured on one of your devices. Please, take actions.\n\n{message}";
            string subject = "!!!DEVICE ERROR OCCURED!!!";

            await SendMessageToEmailsAsync(body, subject);
        }
        static private async Task SendMessageToEmailsAsync(string body, string subject)
        {
            if (emailClient == null)
            {
                throw new InvalidDataException("Unable to connect to Azure Communication Services. Please verify your connection string.");
            }
            try
            {
                EmailRecipients emailRecipients = new EmailRecipients(recipients);
                EmailContent emailContent = new EmailContent(subject);
                emailContent.PlainText = body;
                EmailMessage emailMessage = new EmailMessage(sender, emailRecipients, emailContent);

                EmailSendOperation emailSendOperation = await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
                Console.WriteLine($"{DateTime.Now}: Notification about an error has been sent successfully.");
            }
            catch (RequestFailedException ex)
            {
                throw new RequestFailedException("Invalid email sender username. Please use a username from the list of valid usernames configured by your admin.");
            }
        }
    }
}
