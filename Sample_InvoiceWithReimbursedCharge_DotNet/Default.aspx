<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Sample_InvoiceWithReimbursedCharge_DotNet.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
     <% if (dictionary.ContainsKey("accessToken") && dictionary["callMadeBy"]!="OpenId")
        {
            Response.Write("<script> window.opener.location.reload();window.close(); </script>");
        }
    %> 
</head>
<body>
    <form id="form1" runat="server">
        <div id="connect" runat="server" visible ="false">
            <!-- Get App Now -->
            <b>Get App Now</b><br />
            <asp:ImageButton id="btnOpenId" runat="server" AlternateText="Get App Now"
                    ImageAlign="left"
                    ImageUrl="Images/Get_App.png"
                    OnClick="ImgOpenId_Click" CssClass="font-size:14px; border: 1px solid grey; padding: 10px; color: red" Height="40px" Width="200px"/>
            <br /><br /><br />   
        </div>
        <div id="revoke" runat="server" visible ="false">
            <p>
                <asp:label runat="server" id="lblConnected" visible="false">"Your application is connected!"</asp:label>
            </p>  

            <p>
                <asp:Label runat="server">"This sample app demonstartes a workflow for Billable expenses (Reimbursed charge) and Invoices, . To read more about it go to: <a href='https://developer.intuit.com/hub/blog/2017/06/21/reimburse-charge-support-quickbooks-online-api'>blog on Reimbursed Charge</a>
                    The flow showed in this sample app is as follows."</asp:Label>
                <asp:Label runat="server">"Note: In case of issues, please check logs in the filepath you mentioned or debug the code."</asp:Label>
                <asp:ListBox runat="server" ID="Label2" Height="150px" >
                    <asp:ListItem Text="1. Click first button here to create Item and Account based Billable Expense." />
                    <asp:ListItem Text="2. Click invoice button to create an Invoice with just a description for the same customer as in Expense created before." />
                    <asp:ListItem>"3. UI step: Go to QBO UI to the invoice just created (see id after it has been created) and in the right drawer window, add the two item to the invoice and hit save. This is a manual linking since the API does not support creation of this link yet.</asp:ListItem>
                    <asp:ListItem>To see what links are supported by API check here: <a href='https://developer.intuit.com/docs/00_quickbooks_online/2_build/60_tutorials/0030_manage_linked_transactions'>Other linked transactions guide</a>"</asp:ListItem>
                    <asp:ListItem Text="4. Enter a valid email to send this linked invoice to be sent to that email." />
                    <asp:ListItem Text="5. After the invoice has been paid for, click on Receive Payment to record the payment in QBO." />
                </asp:ListBox>
            </p>

            <asp:Button id="btnQBOAPICall" runat="server" OnClick="ExpenseBtnCall_Click" Text="Billable Expense" />
            <p>
                <asp:label runat="server" id="lblQBOCall" visible="false">"Expense Id here"</asp:label>
            </p>
            <br /><br />

            <asp:Button id="createInvoice" runat="server" OnClick="InvoiceBtn_Click" Text="Create Invoice button" />
            <p>
                <asp:label runat="server" id="showInvoiceId" visible="false">"Invoice Id here"</asp:label>
            </p> 
            <br /><br />

            <p>
                <asp:label runat="server" id="linkInvoiceMessage" visible="true">Please link expense and invoice with ids shown above.</asp:label>
            </p> 
            <br /><br />

            <div>
                <span><asp:TextBox ID="EmailText" runat="server" Text="Enter email id here" /></span>
                <span><asp:Button id="sendEmail" runat="server" OnClick="SendEmail_Click" Text="Send Email button" /></span>
                <br /><br />
            </div>

            <asp:Button id="receivePayment" runat="server" OnClick="PaymentBtn_Click" Text="Receive Payment" />
            <p>
                <asp:label runat="server" id="showPaymentId" visible="false">"Pay"</asp:label>
            </p> 
            <br /><br />

            <asp:Button id="btnRevoke" runat="server" OnClick="ImgRevoke_Click" Text="Access Token Revoke button" />
            <br /><br /><br />
        </div>
    </form>
</body>
</html>
