using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.DataService;
using Intuit.Ipp.QueryFilter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sample_InvoiceWithReimbursedCharge_DotNet.QBOHelper
{
    public class QBO
    {
        internal static Account QueryOrAddAccount(ServiceContext context, String query, AccountTypeEnum accountType, AccountClassificationEnum classification, AccountSubTypeEnum subType)
        {
            
            DataService service = new DataService(context);
            QueryService<Account> entityQuery = new QueryService<Account>(context);
            List<Account> queryResponse = entityQuery.ExecuteIdsQuery(query).ToList<Account>();

            if (queryResponse.Count == 0)
            {
                Account account = AccountCreate(context, accountType, classification, subType);
                return account;
            }

            return queryResponse[0];
        }

        internal static Invoice QueryInvoice(ServiceContext context, String query, Customer customer)
        {
            DataService service = new DataService(context);
            QueryService<Invoice> entityQuery = new QueryService<Invoice>(context);
            List<Invoice> queryResponse = entityQuery.ExecuteIdsQuery(query).ToList<Invoice>();

            if (queryResponse.Count == 0)
            {
                Invoice invoice = QBOApp.InvoiceCreate(context, customer);
                return invoice;
            }
            return queryResponse[0];
        }

        internal static Vendor QueryOrAddVendor(ServiceContext context)
        {
            DataService service = new DataService(context);
            QueryService<Vendor> entityQuery = new QueryService<Vendor>(context);
            List<Vendor> queryResponse = entityQuery.ExecuteIdsQuery("Select * from Vendor").ToList<Vendor>();

            if (queryResponse.Count == 0)
            {
                Vendor vendor = VendorCreate(context);
                return vendor;
            }

            return queryResponse[0];
        }

        internal static Account AccountCreate(ServiceContext context, AccountTypeEnum accountType, AccountClassificationEnum classification, AccountSubTypeEnum subType)
        {
            Account account = new Account
            {
                Name = "Account_" + Helper.GetGuid(),
                AccountType = accountType,
                AccountTypeSpecified = true,
                Classification = classification,
                ClassificationSpecified = true,
                AccountSubType = subType.ToString(),
                SubAccountSpecified = true
            };
            Account apiResponse = Helper.AddToQBO(context, account);
            return apiResponse;
        }

        internal static Customer CustomerCreate(ServiceContext context)
        {
            Customer customer = new Customer
            {
                DisplayName = "Customer_" + Helper.GetGuid()
            };
            Customer apiResponse = Helper.AddToQBO(context, customer);
            return apiResponse;
        }

        internal static Vendor VendorCreate(ServiceContext context)
        {
            Vendor vendor = new Vendor
            {
                DisplayName = "Vendor_" + Helper.GetGuid()
            };
            Vendor apiResponse = Helper.AddToQBO(context, vendor);
            return apiResponse;
        }

        internal static Item ItemCreate(ServiceContext context)
        {
            Account expenseAccount = QueryOrAddAccount(context, "select * from account where AccountType='Cost of Goods Sold'", AccountTypeEnum.CostofGoodsSold, AccountClassificationEnum.Expense, AccountSubTypeEnum.SuppliesMaterialsCogs);
            Account incomeAccount = QueryOrAddAccount(context, "select * from account where AccountType='Income'", AccountTypeEnum.Income, AccountClassificationEnum.Revenue, AccountSubTypeEnum.SalesOfProductIncome);
            Item item = new Item
            {
                Name = "Item_" + Helper.GetGuid(),
                ExpenseAccountRef = new ReferenceType { name = expenseAccount.Name, Value = expenseAccount.Id },
                IncomeAccountRef = new ReferenceType { name = incomeAccount.Name, Value = incomeAccount.Id },
                Type = ItemTypeEnum.NonInventory,
                TypeSpecified = true,
                UnitPrice = new Decimal(100.0),
                UnitPriceSpecified = true
            };

            Item apiResponse = Helper.AddToQBO(context, item);
            return apiResponse;
        }

        internal static Invoice QueryInvoice(ServiceContext context, object p, IEntity customer)
        {
            throw new NotImplementedException();
        }
    }

    #region Helper class
    public class Helper
    {
        internal static T AddToQBO<T>(ServiceContext context, T entity) where T : IEntity
        {
            DataService service = new DataService(context);

            T added = service.Add<T>(entity);
            return added;
        }

        internal static String GetGuid()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
    #endregion
}