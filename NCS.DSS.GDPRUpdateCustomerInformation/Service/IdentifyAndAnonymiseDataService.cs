﻿using Microsoft.Extensions.Logging;
using NCS.DSS.GDPRUpdateCustomerInformation.Cosmos.Provider;
using NCS.DSS.GDPRUpdateCustomerInformation.DB;

namespace NCS.DSS.GDPRUpdateCustomerInformation.Service
{
    public class IdentifyAndAnonymiseDataService : IIdentifyAndAnonymiseDataService
    {
        private readonly string _GDPRUpdateCustomersStoredProcedureName = Environment.GetEnvironmentVariable("GDPRUpdateCustomersStoredProcedureName");
        private readonly string _GDPRIdentifyCustomersStoredProcedureName = Environment.GetEnvironmentVariable("GDPRIdentifyCustomersStoredProcedureName");

        private readonly IAzureSqlDbProvider _azureSqlDbProvider;
        private readonly ILogger<IIdentifyAndAnonymiseDataService> _logger;
        private readonly IDocumentDBProvider _documentDbProvider;

        public IdentifyAndAnonymiseDataService(
            IDocumentDBProvider documentDbProvider,
            IAzureSqlDbProvider azureSqlDbProvider,
            ILogger<IIdentifyAndAnonymiseDataService> logger)
        {
            _azureSqlDbProvider = azureSqlDbProvider;
            _documentDbProvider = documentDbProvider;
            _logger = logger;
        }

        public async Task AnonymiseData()
        {
            await _azureSqlDbProvider.ExecuteStoredProcedureAsync(_GDPRUpdateCustomersStoredProcedureName);
        }

        public async Task DeleteCustomersFromCosmos(List<Guid> customerIdList)
        {
            if (customerIdList != null)
            {
                foreach (var id in customerIdList)
                {
                    await _documentDbProvider.DeleteRecordsForCustomer(id);
                }
            }
        }

        //temporary method to return list of customer ids for testing
        public async Task<List<Guid>> ReturnCustomerIds()
        {
            return await _azureSqlDbProvider.ExecuteStoredProcedureAsync(_GDPRIdentifyCustomersStoredProcedureName);
        }
    }
}
