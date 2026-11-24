namespace NCS.DSS.DataUtility.Interfaces
{
    public interface ICosmosDatabaseService
    {
        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeActionPlansForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeActionsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeAddressesForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeContactDetailsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeCustomerRecordAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeDiversityDetailsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeEmploymentProgressionsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeGoalsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeInteractionsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeLearningProgressionsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeOutcomesForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeSessionsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeSubscriptionsForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeTransfersForCustomerAsync(Guid customerId, int ttl);

        Task<(bool processedSuccessfully, int impactedRecordCount)> PurgeWebchatsForCustomerAsync(Guid customerId, int ttl);

        Task<bool> PurgeDocumentFromCosmosAsync(Guid documentId, string databaseId, string containerId);

        Task DeleteGenericRecordsFromContainer(string databaseName, string containerName, string field, string value, bool int_bool);
    }
}
