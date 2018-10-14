using Moq;
using NUnit.Framework;
using Optional;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Optional.Linq;
using FluentAssertions;

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


        private Func<Option<string>> GetApiKeyFromHeader;

        public MainTests()
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

            GetApiKeyFromHeader = () => "mockApiKey".Some();

            _applicationProviderMock
                .Setup(v => v.GetClientApplicationCode(It.IsAny<string>()))
                .Returns("appCode".Some());

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

            var result = from apiKey in GetApiKeyFromHeader()
                         from appCode in _applicationProvider.GetClientApplicationCode(apiKey)  // tutaj nie był dozwolony zwrot "string" musiał być "Option<string>"
                         select appCode;

            //var apiKey = GetApiKeyFromHeader();
            //if (apiKey == null)
            //    return BadRequest("API Key is missing in headers.");

            //var appCode = _applicationProvider.GetClientApplicationCode(apiKey);
            //if (appCode == null)
            //    return BadRequest("Unknown application");

            //var result = await _paymentDomainService.StartPaymentAsync(request, appCode);
            return Ok(result.ValueOr(string.Empty));
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
        Option<string> GetClientApplicationCode(string apiKey);
    }

    public interface IPaymentDomainService
    {
        Task<string> StartPaymentAsync(StartPaymentRequest request, string appCode);
    }
}
