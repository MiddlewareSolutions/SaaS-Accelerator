﻿using System;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.Services.Models;

namespace Marketplace.SaaS.Accelerator.Services.Helpers;

/// <summary>
/// Email Helper.
/// </summary>
public class EmailHelper
{
    private readonly IApplicationConfigRepository applicationConfigRepository;
    private readonly ISubscriptionsRepository subscriptionsRepository;
    private readonly IEmailTemplateRepository emailTemplateRepository;
    private readonly IEventsRepository eventsRepository;
    private readonly IPlanEventsMappingRepository planEventsMappingRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailHelper"/> class.
    /// </summary>
    /// <param name="applicationConfigRepository">The application configuration repository.</param>
    /// <param name="subscriptionsRepository">The subscriptions repository.</param>
    /// <param name="emailTemplateRepository">The email template repository.</param>
    /// <param name="planEventsMappingRepository">The plan events mapping repository.</param>
    /// <param name="eventsRepository">The events repository.</param>
    public EmailHelper(
        IApplicationConfigRepository applicationConfigRepository,
        ISubscriptionsRepository subscriptionsRepository,
        IEmailTemplateRepository emailTemplateRepository,
        IPlanEventsMappingRepository planEventsMappingRepository,
        IEventsRepository eventsRepository)
    {
        this.applicationConfigRepository = applicationConfigRepository;
        this.subscriptionsRepository = subscriptionsRepository;
        this.emailTemplateRepository = emailTemplateRepository;
        this.eventsRepository = eventsRepository;
        this.planEventsMappingRepository = planEventsMappingRepository;
    }

    /// <summary>
    /// Prepares the content of the email.
    /// </summary>
    /// <param name="subscriptionID">The subscription identifier.</param>
    /// <param name="planGuId">The plan gu identifier.</param>
    /// <param name="processStatus">The process status.</param>
    /// <param name="planEventName">Name of the plan event.</param>
    /// <param name="subscriptionStatus">The subscription status.</param>
    /// <returns>
    /// Email Content Model.
    /// </returns>
    /// <exception cref="Exception">Error while sending an email, please check the configuration.
    /// or
    /// Error while sending an email, please check the configuration.</exception>
    public EmailContentModel PrepareEmailContent(Guid subscriptionID, Guid planGuId, string processStatus, string planEventName, string subscriptionStatus)
    {
        string body = this.emailTemplateRepository.GetEmailBodyForSubscription(subscriptionID, processStatus);
        var subscriptionEvent = this.eventsRepository.GetByName(planEventName);

        DataAccess.Entities.EmailTemplate emailTemplateData;
        if(processStatus == "failure") {
            emailTemplateData = this.emailTemplateRepository.GetTemplateForStatus("Failed");
        } else {
            emailTemplateData = this.emailTemplateRepository.GetTemplateForStatus(subscriptionStatus);
        }

        string subject = "";
        bool copyToCustomer = false;
        string toReceipents = "";
        string ccReceipents = "";
        string bccReceipents = "";

        var eventData = this.planEventsMappingRepository.GetPlanEvent(planGuId, subscriptionEvent.EventsId);

        if (eventData != null) {
            toReceipents = eventData.SuccessStateEmails;
            copyToCustomer = Convert.ToBoolean(eventData.CopyToCustomer);
        }

        if (string.IsNullOrEmpty(toReceipents)) {
            throw new Exception(" Error while sending an email: no receipients. Please check the configuration.");
        }

        if (emailTemplateData != null)
        {
            if (!string.IsNullOrEmpty(toReceipents) && !string.IsNullOrEmpty(emailTemplateData.Cc))
            {
                ccReceipents = emailTemplateData.Cc;
            }

            if (!string.IsNullOrEmpty(emailTemplateData.Bcc))
            {
                bccReceipents = emailTemplateData.Bcc;
            }

            subject = emailTemplateData.Subject;
        }

        return FinalizeContentEmail(subject, body, ccReceipents, bccReceipents, toReceipents, copyToCustomer);
    }
    /// <summary>
    /// Prepares the content of the scheduler email.
    /// </summary>
    /// <param name="subscriptionName">The subscription Name.</param>
    /// <param name="schedulerTaskName">scheduler Task Name.</param>
    /// <param name="responseJson">response Json.</param>
    /// <param name="subscriptionStatus">The subscription status.</param>
    /// <returns>
    /// Email Content Model.
    /// </returns>
    /// <exception cref="Exception">Error while sending an email, please check the configuration.
    /// or
    /// Error while sending an email, please check the configuration.</exception>
    public EmailContentModel PrepareMeteredEmailContent(string schedulerTaskName, String subscriptionName, string subscriptionStatus, string responseJson)
    {
        var emailTemplateData = this.emailTemplateRepository.GetTemplateForStatus(subscriptionStatus);
        string toReceipents = this.applicationConfigRepository.GetValueByName("SchedulerEmailTo");
        if (string.IsNullOrEmpty(toReceipents)) {
            throw new Exception(" Error while sending an email: no receipients. Please check the configuration.");
        }
        var body = emailTemplateData.TemplateBody
            .Replace("****SubscriptionName****", subscriptionName)
            .Replace("****SchedulerTaskName****", schedulerTaskName)
            .Replace("****ResponseJson****", responseJson);
        // return email with content
        return FinalizeContentEmail(emailTemplateData.Subject, body, string.Empty, string.Empty, toReceipents, false);
    }

    private EmailContentModel FinalizeContentEmail(string subject, string body, string ccEmails,string bcEmails, string toEmails, bool copyToCustomer)
    {
        return new EmailContentModel() {
            Subject = subject,
            Body = body,
            BCCEmails = bcEmails,
            CCEmails = ccEmails,
            ToEmails = toEmails,
            IsActive = false,
            CopyToCustomer = copyToCustomer,
            FromEmail = this.applicationConfigRepository.GetValueByName("SMTPFromEmail"),
            Password = this.applicationConfigRepository.GetValueByName("SMTPPassword"),
            SSL = bool.Parse(this.applicationConfigRepository.GetValueByName("SMTPSslEnabled")),
            UserName = this.applicationConfigRepository.GetValueByName("SMTPUserName"),
            Port = int.Parse(this.applicationConfigRepository.GetValueByName("SMTPPort")),
            SMTPHost = this.applicationConfigRepository.GetValueByName("SMTPHost")
        };
    }


}