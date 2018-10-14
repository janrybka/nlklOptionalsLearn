using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using FluentAssertions;

namespace BC.NPP.Nlkl.Optional.OrgVer.Tests
{
    [TestFixture]
    public class OryginalVersionTests
    {
        private readonly Mock<IStartPaymentValidator> _startPaymentValidatorMock;
        private readonly IStartPaymentValidator _startPaymentValidator;
        private readonly Mock<IApplicationProvider> _applicationProviderMock;
        private readonly IApplicationProvider _applicationProvider;
        private readonly Mock<IPaymentDomainService> _paymentDomainServiceMock;
        private readonly IPaymentDomainService _paymentDomainService;
        
        private Func<string> GetApiKeyFromHeader;

        public OryginalVersionTests()
        {
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

            GetApiKeyFromHeader = () => "mockApiKey";

            _applicationProviderMock
                .Setup(v => v.GetClientApplicationCode(It.IsAny<string>()))
                .Returns("appCode");

            _paymentDomainServiceMock
                .Setup(v => v.StartPaymentAsync(It.IsAny<StartPaymentRequest>(), It.IsAny<string>()))
                .Returns(Task.FromResult("result"));

            //Act
            var result = await StartPayment(new StartPaymentRequest());

            //Assert
            result.Should().BeOfType<OkResult>();
        }

        ///TODO: 
        ///- Co zrobić a async? Czy zadziała?
        ///- przyda się jeszcze zapis z FlatMap
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
