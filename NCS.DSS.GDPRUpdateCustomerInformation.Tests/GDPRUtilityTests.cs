using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NCS.DSS.GDPRUtility.Services;

namespace NCS.DSS.GDPRUtility.Tests
{
    public class GDPRUtilityTests
    {
        private readonly IIdentifyAndAnonymiseDataService _fakeDataService;
        private readonly ILogger<Function.GDPRUtility> _fakeLogger;
        private readonly Function.GDPRUtility _function;
        private readonly HttpRequest _request;

        public GDPRUtilityTests()
        {
            _fakeDataService = A.Fake<IIdentifyAndAnonymiseDataService>();
            _fakeLogger = A.Fake<ILogger<Function.GDPRUtility>>();
            _function = new Function.GDPRUtility(_fakeDataService, _fakeLogger);
            _request = new DefaultHttpContext().Request;
        }

        [Fact]
        public async Task Run_NoCustomers_NoOperationsPerformed()
        {
            // Arrange
            A.CallTo(() => _fakeDataService.ReturnCustomerIds()).Returns(Task.FromResult(new List<Guid>()));

            // Act
            await _function.RunAsync(_request);

            // Assert
            A.CallTo(() => _fakeDataService.AnonymiseData()).MustNotHaveHappened();
            A.CallTo(() => _fakeDataService.DeleteCustomersFromCosmos(A<List<Guid>>.Ignored)).MustNotHaveHappened();
        }

        [Fact]
        public async Task Run_CustomersExist_OperationsPerformed()
        {
            // Arrange
            var customerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            A.CallTo(() => _fakeDataService.ReturnCustomerIds()).Returns(Task.FromResult(customerIds));

            // Act
            await _function.RunAsync(_request);

            // Assert
            A.CallTo(() => _fakeDataService.AnonymiseData()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeDataService.DeleteCustomersFromCosmos(customerIds)).MustHaveHappenedOnceExactly();
        }
    }
}