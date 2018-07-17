using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Configuration;
using Intuit.Ipp.OAuth2PlatformClient;
using Intuit.Ipp.Exception;
using Intuit.Ipp.Core;
using System.Net.Http;
using Intuit.Ipp.Data;
using Intuit.Ipp.Security;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;

namespace Sample_InvoiceWithReimbursedCharge_DotNet
{
    public partial class Default : System.Web.UI.Page
    {
        // OAuth2 client configuration
        static string redirectURI = ConfigurationManager.AppSettings["redirectURI"];
        static string clientID = ConfigurationManager.AppSettings["clientID"];
        static string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
        static string env = ConfigurationManager.AppSettings["appEnvironment"];
        static string logPath = ConfigurationManager.AppSettings["logPath"];
        static OAuth2Client oauthClient = new OAuth2Client(clientID, clientSecret, redirectURI, env);

        static string realmId;
        static string authCode;
        static Customer customer;
        static Invoice invoice;
        public static Dictionary<string, string> dictionary = new Dictionary<string, string>();

        protected void Page_PreInit(object sender, EventArgs e)
        {
            if (!dictionary.ContainsKey("accessToken"))
            {
                oauth.Visible = true;
                connected.Visible = false;
            }
            else
            {
                oauth.Visible = false;
                connected.Visible = true;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            AsyncMode = true;
            if (!dictionary.ContainsKey("accessToken"))
            {
                if (Request.QueryString.Count > 0)
                {
                    var response = new AuthorizeResponse(Request.QueryString.ToString());
                    if (response.State != null)
                    {
                        if (oauthClient.CSRFToken == response.State)
                        {
                            if (response.RealmId != null)
                            {
                                realmId = response.RealmId;
                                if (!dictionary.ContainsKey("realmId"))
                                {
                                    dictionary.Add("realmId", realmId);
                                }
                            }

                            if (response.Code != null)
                            {
                                authCode = response.Code;
                                output("Authorization code obtained.");
                                PageAsyncTask t = new PageAsyncTask(performCodeExchange);
                                Page.RegisterAsyncTask(t);
                                Page.ExecuteRegisteredAsyncTasks();
                            }
                        }
                        else
                        {
                            output("Invalid State");
                            dictionary.Clear();
                        }
                    }
                }
            }
            else
            {
                oauth.Visible = false;
                connected.Visible = true;
            }
        }
        #region button click events

        protected void ImgOAuth_Click(object sender, ImageClickEventArgs e)
        {
            try
            {
                if (!dictionary.ContainsKey("accessToken"))
                {
                    List<OidcScopes> scopes = new List<OidcScopes>();
                    scopes.Add(OidcScopes.Accounting);
                    var authorizationRequest = oauthClient.GetAuthorizationURL(scopes);
                    Response.Redirect(authorizationRequest, "_blank", "menubar=0,scrollbars=1,width=780,height=900,top=10");
                }
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        protected async void ExpenseBtnCall_Click(object sender, EventArgs e)
        {
            if (dictionary.ContainsKey("realmId") && dictionary.ContainsKey("accessToken"))
            {
                Action<ServiceContext> apiCallFucntion = new Action<ServiceContext>(CreateExpenseCall);
                await QBOApiCall(apiCallFucntion);
            }
            else
            {
                lblQBOCall.Visible = true;
                lblQBOCall.Text = "Access token not found.";
            }
        }

        protected async void InvoiceBtn_Click(object sender, EventArgs e)
        {
            if (dictionary.ContainsKey("realmId") && dictionary.ContainsKey("accessToken"))
            {
                Action<ServiceContext> apiCallFucntion = new Action<ServiceContext>(CreateInvoiceCall);
                await QBOApiCall(apiCallFucntion);
            }
            else
            {
                lblQBOCall.Visible = true;
                lblQBOCall.Text = "Access token not found.";
            }

        }

        protected async void SendEmail_Click(object sender, EventArgs e)
        {
            if (dictionary.ContainsKey("realmId") && dictionary.ContainsKey("accessToken"))
            {
                Action<ServiceContext> apiCallFucntion = new Action<ServiceContext>(SendEmailCall);
                await QBOApiCall(apiCallFucntion);
            }
            else
            {
                lblQBOCall.Visible = true;
                lblQBOCall.Text = "Access token not found.";
            }
        }

        protected async void PaymentBtn_Click(object sender, EventArgs e)
        {
            if (dictionary.ContainsKey("realmId") && dictionary.ContainsKey("accessToken"))
            {
                Action<ServiceContext> apiCallFucntion = new Action<ServiceContext>(ReceivePaymentCall);
                await QBOApiCall(apiCallFucntion);
            }
            else
            {
                lblQBOCall.Visible = true;
                lblQBOCall.Text = "Access token not found.";
            }
        }
        #endregion

        /// <summary>
        /// Exchange auth code to get Bearer token
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task performCodeExchange()
        {
            output("Exchanging code for tokens.");
            try
            {
                var tokenResp = await oauthClient.GetBearerTokenAsync(authCode);
                if (!dictionary.ContainsKey("accessToken"))
                    dictionary.Add("accessToken", tokenResp.AccessToken);
                else
                    dictionary["accessToken"] = tokenResp.AccessToken;

                if (!dictionary.ContainsKey("refreshToken"))
                    dictionary.Add("refreshToken", tokenResp.RefreshToken);
                else
                    dictionary["refreshToken"] = tokenResp.RefreshToken;
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        #region qbo calls
        /// <summary>
        /// Test QBO api call
        /// </summary>
        /// <param name="access_token"></param>
        /// <param name="refresh_token"></param>
        /// <param name="realmId"></param>
        /// <param name="apiCallFunction"></param>
        public async System.Threading.Tasks.Task QBOApiCall(Action<ServiceContext> apiCallFunction)
        {
            try
            {
                if (realmId != "")
                {
                    output("Making QBO API Call.");

                    if (dictionary["accessToken"] != null && dictionary["realmId"] != null)
                    {
                        OAuth2RequestValidator reqValidator = new OAuth2RequestValidator(dictionary["accessToken"]);
                        ServiceContext context = new ServiceContext(dictionary["realmId"], IntuitServicesType.QBO, reqValidator);
                        context.IppConfiguration.MinorVersion.Qbo = "24";
                        apiCallFunction(context);
                    }
                    else
                    {
                        output("Access token not found.");
                    }
                }
            }
            catch (IdsException ex)
            {
                if (ex.Message == "Unauthorized-401")
                {
                    output("Invalid/Expired Access Token.");

                    var tokenResp = await oauthClient.RefreshTokenAsync(dictionary["refreshToken"]);
                    if (tokenResp.AccessToken != null && tokenResp.RefreshToken != null)
                    {
                        dictionary["accessToken"] = tokenResp.AccessToken;
                        dictionary["refreshToken"] = tokenResp.RefreshToken;
                        await QBOApiCall(apiCallFunction);
                    }
                    else
                    {
                        output("Error while refreshing tokens: " + tokenResp.Raw);
                    }
                }
                else
                {
                    output(ex.Message);
                }
            }
            catch (Exception ex)
            {
                output("Invalid/Expired Access Token.");
            }
        }

        /// <summary>
        /// Call Create invoice from QBOApp
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public void CreateInvoiceCall(ServiceContext context)
        {
            try
            {
                DataService dataService = new DataService(context);
                QueryService<Account> queryService = new QueryService<Account>(context);
                invoice = QBOApp.InvoiceCreate(dataService, queryService, customer);
                output("QBO Invoice call successful.");
                showInvoiceId.Visible = true;
                showInvoiceId.Text = "Created invoice with id: "+ invoice.Id;
                output("Invoice with Id " + invoice.Id + " created");
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        /// <summary>
        /// Call Create expense from QBOApp
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public void CreateExpenseCall(ServiceContext context)
        {
            try
            {
                DataService dataService = new DataService(context);
                QueryService<Account> queryService = new QueryService<Account>(context);
                customer = QBOApp.CustomerCreate(dataService);
                Purchase purchase = QBOApp.BillableExpenseCreate(dataService, queryService, customer);
                output("QBO Expense call successful.");
                lblQBOCall.Visible = true;
                lblQBOCall.Text = "Created expense with id: " + purchase.Id;
                output("Expense with Id " + purchase.Id + " created");
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        /// <summary>
        /// Call Send email from QBOApp
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public void SendEmailCall(ServiceContext context)
        {
            try
            {
                DataService dataService = new DataService(context);
                QBOApp.EmailInvoice(dataService, invoice, EmailText.Text);
                output("QBO Email invoice call successful.");
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        /// <summary>
        /// Call Record payment from QBOApp
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public void ReceivePaymentCall(ServiceContext context)
        {
            try
            {
                DataService dataService = new DataService(context);
                QueryService<Invoice> queryService = new QueryService<Invoice>(context);
                Invoice invoiceLinked = QBOApp.QueryInvoice(queryService, "Select * from Invoice where id='" + invoice.Id + "'");
                Payment payment = QBOApp.ReceivePayment(dataService, invoiceLinked);
                output("QBO Payment received call successful.");
                showPaymentId.Visible = true;
                showPaymentId.Text = "Received payment with id: " + payment.Id;
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }
        #endregion

        #region helper methods for logging
        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        public string GetLogPath()
        {
            try
            {
                if (logPath == "")
                {
                    logPath = System.Environment.GetEnvironmentVariable("TEMP");
                    if (!logPath.EndsWith("\\")) logPath += "\\";
                }
            }
            catch
            {
                output("Log error path not found.");
            }
            return logPath;
        }

        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="logMsg">string to be appended</param>
        public void output(string logMsg)
        {
            //Console.WriteLine(logMsg);

            System.IO.StreamWriter sw = System.IO.File.AppendText(GetLogPath() + "OAuth2SampleAppLogs.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, logMsg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }

        #endregion  
    }

    /// <summary>
    /// Helper for calling self
    /// </summary>
    public static class ResponseHelper
    {
        public static void Redirect(this HttpResponse response, string url, string target, string windowFeatures)
        {
            if ((String.IsNullOrEmpty(target) || target.Equals("_self", StringComparison.OrdinalIgnoreCase)) && String.IsNullOrEmpty(windowFeatures))
            {
                response.Redirect(url);
            }
            else
            {
                Page page = (Page)HttpContext.Current.Handler;
                if (page == null)
                {
                    throw new InvalidOperationException("Cannot redirect to new window outside Page context.");
                }
                url = page.ResolveClientUrl(url);
                string script;
                if (!String.IsNullOrEmpty(windowFeatures))
                {
                    script = @"window.open(""{0}"", ""{1}"", ""{2}"");";
                }
                else
                {
                    script = @"window.open(""{0}"", ""{1}"");";
                }
                script = String.Format(script, url, target, windowFeatures);
                ScriptManager.RegisterStartupScript(page, typeof(Page), "Redirect", script, true);
            }
        }
    }
}