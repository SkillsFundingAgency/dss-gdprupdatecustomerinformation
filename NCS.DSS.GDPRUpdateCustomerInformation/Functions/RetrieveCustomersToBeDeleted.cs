using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.DataUtility.Interfaces;
using NCS.DSS.DataUtility.Models;

namespace NCS.DSS.DataUtility.Functions
{
    public class RetrieveCustomersToBeDeleted
    {
        private readonly ILogger<RetrieveCustomersToBeDeleted> _logger;
        private readonly ISqlDbService _sqlDbService;
        private readonly IServiceBusService _serviceBusService;

        private static int _numberOfSuccesses;
        private static int _numberOfFailures;
        private static string _financialYear;

        private static readonly int[] TargetYears = { 2026, 2027, 2028, 2029, 2030, 2031, 2032, 2033 };

        public RetrieveCustomersToBeDeleted(ILogger<RetrieveCustomersToBeDeleted> logger, ISqlDbService sqlDbService, IServiceBusService serviceBusService)
        {
            _logger = logger;
            _sqlDbService = sqlDbService;
            _serviceBusService = serviceBusService;
        }

        // To understand the current cron trigger, visit https://crontab.guru/
        [Function(nameof(RetrieveCustomersToBeDeleted))]
        public async Task Run([TimerTrigger("%GdprTimerSchedule%")] TimerInfo timer)
        {
            _logger.LogInformation($"Function '{nameof(RetrieveCustomersToBeDeleted)}' has been invoked");
            var targetYearValue = Environment.GetEnvironmentVariable("TargetYear");

            if(!int.TryParse(targetYearValue, out int targetYear))
            {
                throw new InvalidOperationException("TargetYear environment variable is not set or is not a valid integer.");
            }            
                
            if(!TargetYears.Contains(targetYear))
            {
                _logger.LogInformation("Invalid target year provided. Valid target years are: {ValidTargetYears}", string.Join(", ", TargetYears));
                return;
            }

            _financialYear = GetFinancialYear(targetYear);

            try
            {
                _logger.LogInformation("Retrieving customer IDs where the last interaction date was within the financial year '{FinancialYear}'", _financialYear);

                List<Guid> customerIds = await _sqlDbService.RetrieveCustomerIdsAsync(targetYear);

                if (customerIds.Count == 0)
                {
                    _logger.LogInformation("No records found for the financial year '{FinancialYear}'", _financialYear);
                    return;
                }

                _logger.LogInformation("A total of '{CustomerIds}' customers have been identified for the financial year {FinancialYear} to have ttl assigned", customerIds.Count.ToString(), _financialYear);

                _logger.LogInformation("Sending each customer ID onto a service bus queue for processing");

                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 5
                };

                IEnumerable<Guid> customerIdEnumerable = customerIds;
                string queueName = Environment.GetEnvironmentVariable("GdprQueueName");

                await Parallel.ForEachAsync(customerIdEnumerable, options, async (customerId, _) =>
                {
                    DeleteCustomerQueueMessage message = new DeleteCustomerQueueMessage
                    {
                        CustomerId = customerId,
                        TargetYear = targetYear
                    };

                    try
                    {
                        await _serviceBusService.SendQueueMessageAsync(message, queueName);
                        Interlocked.Increment(ref _numberOfSuccesses); // used for thread safety - https://jeremybytes.blogspot.com/2024/02/parallelforeachasync-and-exceptions.html
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ERROR: Failed to send queue message in parallel. Exception: {exception}. Customer ID: {customerId}", ex.Message, customerId);
                        Interlocked.Increment(ref _numberOfFailures);
                    }
                });

                _logger.LogInformation("Total number of queue messages SUCCESSFULLY sent: {NumberOfSuccesses}", _numberOfSuccesses);
                _logger.LogInformation("Total number of queue messages FAILED to be sent: {NumberOfFailures}", _numberOfFailures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "INVOCATION ERROR ({name}): function has failed with exception: {exception}", nameof(RetrieveCustomersToBeDeleted), ex.Message);
                throw;
            }

            _logger.LogInformation($"Function '{nameof(RetrieveCustomersToBeDeleted)}' has finished invocation");
        }

        private string GetFinancialYear(int targetYear)
        {
            if (targetYear > 0)
            {
                return $"{targetYear - 7}/{targetYear - 6}";
            }
            return "";
        }
    }
}
