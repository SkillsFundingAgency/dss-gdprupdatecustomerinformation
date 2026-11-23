namespace NCS.DSS.DataUtility.Interfaces
{
    public interface ISqlDbService
    {
        Task<List<Guid>> RetrieveCustomerIdsAsync(int targetYear);

        Task<int> PurgeDataItemsForCustomerAsync(Guid customerId);

        Task<int> PurgeCustomerDataAsync(Guid customerId);

        Task PurgeRecordDataAsync(Guid recordId, string tableName);
    }
}
