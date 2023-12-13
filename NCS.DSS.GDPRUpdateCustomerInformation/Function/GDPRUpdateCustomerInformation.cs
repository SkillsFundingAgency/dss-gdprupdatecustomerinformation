using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using NCS.DSS.GDPRUpdateCustomerInformation.Service;

namespace NCS.DSS.GDPRUpdateCustomerInformation.Function
{
    public class GDPRUpdateCustomerInformation
    {
        private readonly IIdentifyAndAnonymiseDataService _IdentifyAndAnonymiseDataService;
        private readonly ILogger<GDPRUpdateCustomerInformation> _logger;

        public GDPRUpdateCustomerInformation(IIdentifyAndAnonymiseDataService IdentifyAndAnonymiseDataService, ILogger<GDPRUpdateCustomerInformation> logger)
        {
            _IdentifyAndAnonymiseDataService = IdentifyAndAnonymiseDataService;
            _logger = logger;
        }

        // Currently runs once a year on 04/06 at 2AM
        [FunctionName("GDPRUpdateCustomerInformation")]
        public async Task RunTimer([TimerTrigger("0 0 2 6 4 *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");

            //temporarily commented out to avoid db changes
            //await _IdentifyAndAnonymiseDataService.AnonymiseData();
        }
    }
}
