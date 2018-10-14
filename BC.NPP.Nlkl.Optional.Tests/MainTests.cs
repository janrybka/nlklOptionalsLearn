using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BC.NPP.Nlkl.Optional.Tests
{
    [TestFixture]
    public class MainTests
    {
        private readonly Mock<IStartPaymentValidator> _startPaymentValidatorMock;
        private readonly IStartPaymentValidator _startPaymentValidator;
        private readonly Mock<IApplicationProvider> _applicationProviderMock;
        private readonly IApplicationProvider _applicationProvider;
        private readonly Mock<IPaymentDomainService> _paymentDomainServiceMock;
        private readonly IPaymentDomainService _paymentDomainService;

        private string _apiKey = "mockApiKey";

        public MainTests()
        {
            GetApiKeyFromHeader = () => _apiKey;

            _startPaymentValidatorMock = new Mock<IStartPaymentValidator>();
            _startPaymentValidator = _startPaymentValidatorMock.Object;

            _applicationProviderMock = new Mock<IApplicationProvider>();
            _applicationProvider = _applicationProviderMock.Object;


            _paymentDomainServiceMock = new Mock<IPaymentDomainService>();
            _paymentDomainService = _paymentDomainServiceMock.Object;
        }

        [Test]
        public async Task TestStartPayment()
        {
            //Arrange
            _startPaymentValidatorMock
                .Setup(v => v.Validate(It.IsAny<StartPaymentRequest>()))
                .Returns(true);
            _applicationProviderMock
                .Setup(v => v.GetClientApplicationCode(It.IsAny<string>()))
                .Returns("appCode");
            _paymentDomainServiceMock
                .Setup(v => v.StartPaymentAsync(It.IsAny<StartPaymentRequest>(), It.IsAny<string>()))
                .Returns(Task.FromResult("result"));

            //Ack
            await StartPayment(new StartPaymentRequest());

            //Assert

        }

        public async Task<IActionResult> StartPayment(StartPaymentRequest request)
        {
            if (!_startPaymentValidator.Validate(request))
                return BadRequest();

            var apiKey = GetApiKeyFromHeader();
            if (apiKey == null)
                return BadRequest("API Key is missing in headers.");

            var appCode = _applicationProvider.GetClientApplicationCode(apiKey);
            if (appCode == null)
                return BadRequest("Unknown application");

            var result = await _paymentDomainService.StartPaymentAsync(request, appCode);
            return Ok(result);
        }

        #region utils
        private IActionResult BadRequest(string message = "")
        {
            return new BadResult();
        }

        private IActionResult Ok(string result)
        {
            return new OkResult();
        }

        private Func<string> GetApiKeyFromHeader;
        #endregion
    }

    public interface IActionResult { }
    public class BadResult : IActionResult { }
    public class OkResult : IActionResult { }

    public class StartPaymentRequest { }

    public interface IStartPaymentValidator
    {
        bool Validate(StartPaymentRequest request);
    }

    public interface IApplicationProvider
    {
        string GetClientApplicationCode(string apiKey);
    }

    public interface IPaymentDomainService
    {
        Task<string> StartPaymentAsync(StartPaymentRequest request, string appCode);
    }
}
