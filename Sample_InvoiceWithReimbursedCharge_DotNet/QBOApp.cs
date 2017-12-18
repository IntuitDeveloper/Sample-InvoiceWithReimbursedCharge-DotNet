using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Intuit.Ipp.Security;
using Intuit.Ipp.Core;
using System.Configuration;
using System.Net;
using System.Globalization;
using System.IO;
using Intuit.Ipp.Exception;
using Intuit.Ipp.Data;
using Intuit.Ipp;
using Intuit.Ipp.DataService;


namespace Sample_InvoiceWithReimbursedCharge_DotNet
{
    public class QBOApp
    {
        /// <summary>
        /// Create Billable expense
        /// </summary>
        /// <param name="context"></param>
        /// <param name="customer"></param>
        /// <returns>purchase</returns>
        public static Purchase BillableExpenseCreate(ServiceContext context, Customer customer)
        {
            Account account = QBOHelper.QBO.QueryOrAddAccount(context, "select * from account where AccountSubType='Checking'", AccountTypeEnum.Bank, AccountClassificationEnum.Asset, AccountSubTypeEnum.Checking);
            Account expenseAccount = QBOHelper.QBO.QueryOrAddAccount(context, "select * from account where AccountType='Expense'", AccountTypeEnum.Expense, AccountClassificationEnum.Expense, AccountSubTypeEnum.AdvertisingPromotional);
            Vendor vendor = QBOHelper.QBO.QueryOrAddVendor(context);
            Item item = QBOHelper.QBO.ItemCreate(context);
            Purchase purchase = new Purchase
            {
                PaymentType = PaymentTypeEnum.Cash,
                PaymentTypeSpecified = true,
                AccountRef = new ReferenceType { name = account.Name, Value = account.Id },
                EntityRef = new ReferenceType { type = EntityTypeEnum.Vendor.ToString(), name = vendor.DisplayName, Value = vendor.Id }
            };

            Line itemLine = new Line
            {
                Description = "Item based expense line detail.",
                DetailType = LineDetailTypeEnum.ItemBasedExpenseLineDetail,
                DetailTypeSpecified = true
            };

            MarkupInfo markupInfo = new MarkupInfo
            {
                PercentBased = true,
                PercentBasedSpecified = true,
                PercentSpecified = true,
                Percent = new Decimal(50)
            };

            ItemBasedExpenseLineDetail itemLineDetail = new ItemBasedExpenseLineDetail
            {
                CustomerRef = new ReferenceType { name = customer.DisplayName, Value = customer.Id },
                BillableStatus = BillableStatusEnum.Billable,
                BillableStatusSpecified = true,
                ItemRef = new ReferenceType { name = item.Name, Value = item.Id },
                MarkupInfo = markupInfo,
            };

            itemLine.AnyIntuitObject = itemLineDetail;
            if(item.UnitPrice > 0)
            {
                itemLine.Amount = item.UnitPrice;
            }
            else
            {
                itemLine.Amount = new Decimal(100);
            }
            
            itemLine.AmountSpecified = true;

            Line accountLine = new Line
            {
                Description = "Account based expense line detail.",
                DetailType = LineDetailTypeEnum.AccountBasedExpenseLineDetail,
                DetailTypeSpecified = true
            };

            AccountBasedExpenseLineDetail accountLineDetail = new AccountBasedExpenseLineDetail
            {
                CustomerRef = new ReferenceType { name = customer.DisplayName, Value = customer.Id },
                AccountRef = new ReferenceType { name = expenseAccount.Name, Value = expenseAccount.Id },
                BillableStatus = BillableStatusEnum.Billable,
                BillableStatusSpecified = true,
                MarkupInfo = markupInfo,
            };

            accountLine.AnyIntuitObject = accountLineDetail;
            accountLine.Amount = new Decimal(100);
            accountLine.AmountSpecified = true;

            Line[] lines = { itemLine, accountLine };
            purchase.Line = lines;

            // Add created purchase in QBO
            Purchase apiResponse = QBOHelper.Helper.AddToQBO(context, purchase);
            return apiResponse;

        }

        /// <summary>
        /// Create Invoice
        /// </summary>
        /// <param name="context"></param>
        /// <param name="customer"></param>
        /// <returns>invoice</returns>
        public static Invoice InvoiceCreate(ServiceContext context, Customer customer)
        {
            Item item = QBOHelper.QBO.ItemCreate(context);
            Line line = new Line
            {
                DetailType = LineDetailTypeEnum.SalesItemLineDetail,
                DetailTypeSpecified = true,
                Description = "Sample for Reimburse Charge with Invoice.",
                Amount = new Decimal(40),
                AmountSpecified = true
               
            };
            SalesItemLineDetail lineDetail = new SalesItemLineDetail
            {
                ItemRef = new ReferenceType { name = item.Name, Value = item.Id }
            };
            line.AnyIntuitObject = lineDetail;

            Line[] lines = { line };

            Invoice invoice = new Invoice
            {
                Line = lines,
                CustomerRef = new ReferenceType { name = customer.DisplayName, Value = customer.Id },
                TxnDate = DateTime.Now.Date
            };

            Invoice apiResponse = QBOHelper.Helper.AddToQBO(context, invoice);
            return apiResponse;
        }

        /// <summary>
        /// Email invoice
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invoice"></param>
        /// <param name="emailId"></param>
        /// <returns></returns>
        public static void EmailInvoice(ServiceContext context, Invoice invoice, String emailId)
        {
            DataService service = new DataService(context);
            Invoice sent = service.SendEmail<Invoice>(invoice, emailId);
            //return sent;
        }

        /// <summary>
        /// Record payment on invoice in QBO
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invoice"></param>
        /// <returns>payment</returns>
        public static Payment ReceivePayment(ServiceContext context, Invoice invoice)
        {
            Payment newPayment = new Payment
            {
                TxnDate = DateTime.Now.Date,
                TxnDateSpecified = true,
                CustomerRef = invoice.CustomerRef,
                PaymentType = PaymentTypeEnum.Cash
            };

            LinkedTxn linkInvoice = new LinkedTxn
            {
                TxnId = invoice.Id,
                TxnType = TxnTypeEnum.Invoice.ToString()
            };
            List<LinkedTxn> linkedTxnList = new List<LinkedTxn>
            {
                linkInvoice
            };

            Line line = new Line
            {
                Amount = invoice.TotalAmt,
                AmountSpecified = true
            };
            line.LinkedTxn = linkedTxnList.ToArray();

            List<Line> lines = new List<Line>();
            lines.Add(line);
            newPayment.Line = lines.ToArray();
            newPayment.TotalAmt = invoice.TotalAmt;
            newPayment.TotalAmtSpecified = true;
            Payment apiResponse = QBOHelper.Helper.AddToQBO(context, newPayment);
            return apiResponse;
        }     
    }
}