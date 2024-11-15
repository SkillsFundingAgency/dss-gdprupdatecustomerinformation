using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.GDPRUtility.Services;

namespace NCS.DSS.GDPRUtility.Function
{
    public class GDPRUtility
    {
        private readonly IIdentifyAndAnonymiseDataService _identifyAndAnonymiseDataService;
        private readonly ILogger<GDPRUtility> _logger;

        public GDPRUtility(IIdentifyAndAnonymiseDataService identifyAndAnonymiseDataService, ILogger<GDPRUtility> logger)
        {
            _identifyAndAnonymiseDataService = identifyAndAnonymiseDataService;
            _logger = logger;
        }

        /*
        In case of failed execution, FixedDelayRetry will retry up to three times after a 30 minute delay interval.
        GDPRUtility runs at 2am every month (on the 1st of the month) as defined in NCRONTAB syntax https://crontab.cronhub.io/:
        {seconds} {minutes} {hours} {day of month} {month} {day-of-week}
        */

        [Function(nameof(GDPRUtility))]
        [FixedDelayRetry(3, "00:30:00")] 
        public async Task<IActionResult> RunAsync([TimerTrigger("0 0 2 1 * *")] TimerInfo timer)
        {
            _logger.LogInformation($"{nameof(GDPRUtility)} has been invoked");
            _logger.LogInformation("Attempting to retrieve list of customer IDs");

            try
            {
                List<Guid> customerIds = await _identifyAndAnonymiseDataService.ReturnCustomerIds();

                if (customerIds.Count.Equals(0))
                {
                    _logger.LogInformation("All Customers are GDPR compliant.");
                    return new EmptyResult();
                }

                _logger.LogInformation($"A total of {customerIds.Count().ToString()} Customer IDs have been identified as being non-compliant with GDPR.");

                _logger.LogInformation("Attempting to anonymise data from SQL DB");

                await _identifyAndAnonymiseDataService.AnonymiseData();

                _logger.LogInformation("Successfully anonymised data from SQL DB");

                _logger.LogInformation("Attempting to delete related documents from Cosmos DB");

                await _identifyAndAnonymiseDataService.DeleteCustomersFromCosmos(customerIds);

                _logger.LogInformation($"{nameof(GDPRUtility)} has finished invocation successfully");

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GDPRUtility)} has failed to invoke. Error: {ex.Message}");
                throw;
            }
        }
    }
}
