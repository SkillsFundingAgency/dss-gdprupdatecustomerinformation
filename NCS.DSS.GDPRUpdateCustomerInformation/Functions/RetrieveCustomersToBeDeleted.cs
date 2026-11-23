using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NCS.DSS.DataUtility.Interfaces;
using NCS.DSS.DataUtility.Models;
using System.Net;
using System.Reflection.Metadata.Ecma335;

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

        private string _invalidTargetYear = "Invalid target year provided. Valid target years are: " + string.Join(", ", TargetYears);

        private static readonly int[] TargetYears = { 2026, 2027, 2028, 2029, 2030, 2031, 2032, 2033 };

        public RetrieveCustomersToBeDeleted(ILogger<RetrieveCustomersToBeDeleted> logger, ISqlDbService sqlDbService, IServiceBusService serviceBusService)
        {
            _logger = logger;
            _sqlDbService = sqlDbService;
            _serviceBusService = serviceBusService;
        }

        // To understand the current cron trigger, visit https://crontab.guru/
        [Function(nameof(RetrieveCustomersToBeDeleted))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            var response = req.CreateResponse();
            {
                var targetYear = int.TryParse(req.Query["targetYear"], out int parsedTargetYear) ? parsedTargetYear : 0;
                
                if(!TargetYears.Contains(targetYear))
                {
                    _logger.LogInformation(_invalidTargetYear);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync(_invalidTargetYear);
                    return response;
                }
                _financialYear = GetFinancialYear(targetYear);

                _logger.LogInformation($"Function '{nameof(RetrieveCustomersToBeDeleted)}' has been invoked");

                try
                {
                    _logger.LogInformation("Retrieving customer IDs where the last interaction date was within the financial year '{FinancialYear}'", _financialYear);

                    List<Guid> customerIds = await _sqlDbService.RetrieveCustomerIdsAsync(targetYear);

                    if (customerIds.Count == 0)
                    {
                        _logger.LogInformation("No records found for the financial year '{FinancialYear}'", _financialYear);
                        return response = req.CreateResponse(HttpStatusCode.NoContent);
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

            return response = req.CreateResponse(HttpStatusCode.OK);
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
