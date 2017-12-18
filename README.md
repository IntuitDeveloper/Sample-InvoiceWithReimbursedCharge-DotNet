## DotNet Framework 4.6.1 V3 Sample App with OAuth2
### Sample App in C# that goes over Reimbursed Charge workflow using QBO Accounting API

This sample app is meant to provide working example of how to make API calls to QuickBooks. Please note that while these examples work, features not called out above are not intended to be taken and used in production business applications. In other words, this is not a seed project to be taken cart blanche and deployed to your production environment. 

For example, certain concerns are not addressed at all in our samples (e.g. security, privacy, scalability). In our sample apps, we strive to strike a balance between clarity, maintainability, and performance where we can. However, clarity is ultimately the most important quality in a sample app. Therefore there are certain instances where we might forgo a more complicated implementation (e.g. caching a frequently used value, robust error handling, more generic domain model structure) in favor of code that is easier to read. In that light, we welcome any feedback that makes our samples apps easier to learn from.

Note: This app has been developed and tested on Windows 7 and only deals with US QBO companies. The code will have to be modified for non-US companies. Please check [DotNet CRUD sample app](https://github.com/IntuitDeveloper/SampleApp-CRUD_.Net_Oauth2) to see how to make calls for other entities.

### Table of Contents

* [Running the Application](#running-the-application)
* [Sample Overview](#sample-overview)

### Running the Application

1. Clone the repository on your machine
2. cd to project directory and launch ```Sample_InvoiceWithReimbursedCharge_DotNet``` in Visual Studio
3. Open Web.config and fill details for ```clientId```, ```clientSecret``` and ```logPath``` from App Keys section on your [developer account](https://developer.intuit.com)
4. Copy ```redirectURI``` value in App Keys tab 
5. Build and Run

### Sample Overview

This workflow that this app follows is as follows:
1. Create Item-based and Account-based Billable expense 
2. Create an Invoice with a simple description line item
3. Billable expense is linked with Invoice in UI (this link creation is not currently supported by API)
![img](https://github.com/IntuitDeveloper/Sample_InvoiceWithReimbursedCharge_DotNet/blob/master/Sample_InvoiceWithReimbursedCharge_DotNet/Images/link_billable_expense.png)
4. Email invoice to customer
5. Record a payment on this invoice in QBO

