using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Azure;
using Azure.Communication.Email;

namespace AgentApp
{
    internal static class EmailSender
    {
        static readonly EmailClient emailClient;
        static readonly List<string> recipientsEmailAddresses;
        static readonly string sender;
        static EmailSender()
        {
            sender = "DoNotReply@.azurecomm.net";
            var connectionString = "ccesskeyMGn";
            emailClient = new EmailClient(connectionString);
            recipientsEmailAddresses = new List<string>() { "edu.uni.lodz.pl" };
        }
        static public async Task SendErrorMessageToEmailsAsync(string message)
        {
            if (recipientsEmailAddresses.Count == 0)
            {
                throw new ArgumentException("There are no addresses in the configuration file");
            }
            Console.WriteLine($"Sending message to {recipientsEmailAddresses.Count} email address/addresses");

            string body = $"An error has occured on one of your devices. Please, take actions.\n\n{message}";
            string subject = "!!!ERROR OCCURED!!!";

            foreach (var emailAddress in recipientsEmailAddresses) 
            {
                await SendMessageToSingleEmailAsync(emailAddress, body, subject);
            }
        }
        static private async Task SendMessageToSingleEmailAsync(string recipient, string body, string subject)
        {
            try
            {
                EmailSendOperation operation = await emailClient.SendAsync(Azure.WaitUntil.Completed,
                    sender, recipient, subject, body);

                EmailSendResult status = operation.Value;
                Console.WriteLine($"Message was sent to {recipient}");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
