using System;
using System.Collections.Generic;
using Intuit.Ipp.Data;
using System.Linq;
using Intuit.Ipp;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;

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
        public static Purchase BillableExpenseCreate(DataService dataService, QueryService<Account> queryService, Customer customer)
        {
            Account account = QueryOrAddAccount(dataService, queryService, "select * from account where AccountSubType='Checking'", AccountTypeEnum.Bank, AccountClassificationEnum.Asset, AccountSubTypeEnum.Checking);
            Account expenseAccount = QueryOrAddAccount(dataService, queryService, "select * from account where AccountType='Expense'", AccountTypeEnum.Expense, AccountClassificationEnum.Expense, AccountSubTypeEnum.AdvertisingPromotional);
            Vendor vendor = VendorCreate(dataService);
            Item item = ItemCreate(dataService, queryService);
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
            Purchase apiResponse = dataService.Add(purchase);
            return apiResponse;

        }

        /// <summary>
        /// Create Invoice
        /// </summary>
        /// <param name="context"></param>
        /// <param name="customer"></param>
        /// <returns>invoice</returns>
        public static Invoice InvoiceCreate(DataService dataService, QueryService<Account> queryService, Customer customer)
        {
            Item item = ItemCreate(dataService, queryService);
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

            Invoice apiResponse = dataService.Add(invoice);
            return apiResponse;
        }

        /// <summary>
        /// Email invoice
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invoice"></param>
        /// <param name="emailId"></param>
        /// <returns></returns>
        public static void EmailInvoice(DataService dataService, Invoice invoice, String emailId)
        {
            Invoice sent = dataService.SendEmail<Invoice>(invoice, emailId);
            //return sent;
        }

        /// <summary>
        /// Record payment on invoice in QBO
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invoice"></param>
        /// <returns>payment</returns>
        public static Payment ReceivePayment(DataService dataService, Invoice invoice)
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
            Payment apiResponse = dataService.Add(newPayment);
            return apiResponse;
        }

        internal static Account QueryOrAddAccount(DataService dataService, QueryService<Account> queryService, String query, AccountTypeEnum accountType, AccountClassificationEnum classification, AccountSubTypeEnum subType)
        {
            List<Account> queryResponse = queryService.ExecuteIdsQuery(query).ToList<Account>();

            if (queryResponse.Count == 0)
            {
                Account account = AccountCreate(dataService, accountType, classification, subType);
                return account;
            }
            return queryResponse[0];
        }

        internal static Invoice QueryInvoice(QueryService<Invoice> queryService, String query)
        {
            try
            {
                List<Invoice> queryResponse = queryService.ExecuteIdsQuery(query).ToList<Invoice>();
                return queryResponse[0];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Couldn't find invoice.");
                return null;
            }
        }

        internal static Account AccountCreate(DataService dataService, AccountTypeEnum accountType, AccountClassificationEnum classification, AccountSubTypeEnum subType)
        {
            Account account = new Account
            {
                Name = "Account_" + GetGuid(),
                AccountType = accountType,
                AccountTypeSpecified = true,
                Classification = classification,
                ClassificationSpecified = true,
                AccountSubType = subType.ToString(),
                SubAccountSpecified = true
            };
            Account apiResponse = dataService.Add(account);
            return apiResponse;
        }

        internal static Customer CustomerCreate(DataService dataService)
        {
            Customer customer = new Customer
            {
                DisplayName = "Customer_" + GetGuid()
            };
            Customer apiResponse = dataService.Add(customer);
            return apiResponse;
        }

        internal static Vendor VendorCreate(DataService dataService)
        {
            Vendor vendor = new Vendor
            {
                DisplayName = "Vendor_" + GetGuid()
            };
            Vendor apiResponse = dataService.Add(vendor);
            return apiResponse;
        }

        internal static Item ItemCreate(DataService dataService, QueryService<Account> queryService)
        {
            Account expenseAccount = QueryOrAddAccount(dataService, queryService, "select * from account where AccountType='Cost of Goods Sold'", AccountTypeEnum.CostofGoodsSold, AccountClassificationEnum.Expense, AccountSubTypeEnum.SuppliesMaterialsCogs);
            Account incomeAccount = QueryOrAddAccount(dataService, queryService, "select * from account where AccountType='Income'", AccountTypeEnum.Income, AccountClassificationEnum.Revenue, AccountSubTypeEnum.SalesOfProductIncome);
            Item item = new Item
            {
                Name = "Item_" + GetGuid(),
                ExpenseAccountRef = new ReferenceType { name = expenseAccount.Name, Value = expenseAccount.Id },
                IncomeAccountRef = new ReferenceType { name = incomeAccount.Name, Value = incomeAccount.Id },
                Type = ItemTypeEnum.NonInventory,
                TypeSpecified = true,
                UnitPrice = new Decimal(100.0),
                UnitPriceSpecified = true
            };

            Item apiResponse = dataService.Add(item);
            return apiResponse;
        }

        internal static String GetGuid()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}