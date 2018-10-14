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
        public async Task SuccessStartPaymentFlatMap() => await SuccessStartPayment(true);

        [Test]
        public async Task SuccessStartPaymentLinq() => await SuccessStartPayment(false);

        public async Task SuccessStartPayment(bool testFlatMap)
        {
            //Arrange
            Action setup = () =>
            {
                _startPaymentValidatorMock
                    .Setup(v => v.Validate(It.IsAny<StartPaymentRequest>()))
                    .Returns(new object().Some<object, FailPath>());
            };

            //Act
            var result = await TestStartPayment(testFlatMap, setup);

            //Assert
            result.Should().BeOfType<OkResult>();
        }

        [Test]
        public async Task FailedValidatorStartPaymentFlatMap() => await FailedValidatorStartPayment(true);

        [Test]
        public async Task FailedValidatorStartPaymentLinq() => await FailedValidatorStartPayment(false);

        public async Task FailedValidatorStartPayment(bool testFlatMap)
        {
            //Arrange
            Action setup = () =>
            {
                _startPaymentValidatorMock
                    .Setup(v => v.Validate(It.IsAny<StartPaymentRequest>()))
                    .Returns(Option.None<object, FailPath>(new FailPath("validation error")));
            };

            //Act
            var result = await TestStartPayment(testFlatMap, setup);

            //Assert
            result.Should().BeOfType<BadResult>().And.Match<BadResult>((br) => br.message == "validation error");
        }

        [Test]
        public async Task FailedApiKeyStartPaymentFlatMap() => await FailedApiKeyStartPayment(true);
                          
        [Test]            
        public async Task FailedApiKeyStartPaymentLinq() => await FailedApiKeyStartPayment(false);

        public async Task FailedApiKeyStartPayment(bool testFlatMap)
        {
            //Arrange
            Action setup = () =>
            {
                GetApiKeyFromHeader = () => Option.None<string, FailPath>(new FailPath("apikey error"));
            };

            //Act
            var result = await TestStartPayment(testFlatMap, setup);

            //Assert
            result.Should().BeOfType<BadResult>().And.Match<BadResult>((br) => br.message == "apikey error");
        }


        [Test]
        public async Task FailedAppProviderStartPaymentFlatMap() => await FailedAppProviderStartPayment(true);

        [Test]
        public async Task FailedAppProviderStartPaymentLinq() => await FailedAppProviderStartPayment(false);

        public async Task FailedAppProviderStartPayment(bool testFlatMap)
        {
            //Arrange
            Action setup = () =>
            {
                _applicationProviderMock
                    .Setup(v => v.GetClientApplicationCode(It.IsAny<string>()))
                    .Returns(Option.None<string, FailPath>(new FailPath("AppProvider error")));
            };

            //Act
            var result = await TestStartPayment(testFlatMap, setup);

            //Assert
            result.Should().BeOfType<BadResult>().And.Match<BadResult>((br) => br.message == "AppProvider error");
        }

        [Test]
        public async Task FailedPayServiceStartPaymentFlatMap() => await FailedPayServiceStartPayment(true);

        [Test]
        public async Task FailedPayServiceStartPaymentLinq() => await FailedPayServiceStartPayment(false);

        public async Task FailedPayServiceStartPayment(bool testFlatMap)
        {
            //Arrange
            Action setup = () =>
            {
                _paymentDomainServiceMock
                    .Setup(v => v.StartPaymentAsync(It.IsAny<StartPaymentRequest>(), It.IsAny<string>()))
                    .Returns(Task.Delay(100).ContinueWith((t) => Option.None<string, FailPath>(new FailPath("PayService error"))));
            };

            //Act
            var result = await TestStartPayment(testFlatMap, setup);

            //Assert
            result.Should().BeOfType<BadResult>().And.Match<BadResult>((br) => br.message == "PayService error");
        }

        public async Task<IActionResult> TestStartPayment(bool testFlatMap, Action setup)
        {
            _startPaymentValidatorMock
                .Setup(v => v.Validate(It.IsAny<StartPaymentRequest>()))
                .Returns(new object().Some<object, FailPath>());

            GetApiKeyFromHeader = () => "mockApiKey".Some<string, FailPath>();

            _applicationProviderMock
                .Setup(v => v.GetClientApplicationCode(It.IsAny<string>()))
                .Returns("appCode".Some<string, FailPath>());

            _paymentDomainServiceMock
                .Setup(v => v.StartPaymentAsync(It.IsAny<StartPaymentRequest>(), It.IsAny<string>()))
                .Returns(Task.Delay(100).ContinueWith((t) => "result".Some<string, FailPath>()));

            setup();

            return testFlatMap ? await StartPaymentFlatMap(new StartPaymentRequest()) : await StartPayment(new StartPaymentRequest());
        }

        public async Task<IActionResult> StartPayment(StartPaymentRequest request)
        {
            var appCodeOpt = from validation in _startPaymentValidator.Validate(request)
                          from apiKey in GetApiKeyFromHeader()
                          from appC in _applicationProvider.GetClientApplicationCode(apiKey)  // tutaj nie był dozwolony zwrot "string" musiał być "Option<string>"
                          select appC;

            // Przymusowe rozpakowanie:

            if (!appCodeOpt.HasValue)
            {
                var ex = "";
                appCodeOpt.MapException((fp) => ex = fp.Errors);
                return BadRequest(ex);
            }

            var appCode = "";
            appCodeOpt.MatchSome(aco => appCode = aco);

            // Koniec - tutaj już można użyć async-await

            var startUrl = await _paymentDomainService.StartPaymentAsync(request, appCode);

            IActionResult result = startUrl.Match(
                some: (sUrl) => Ok(sUrl),
                none: (fp) => BadRequest(fp.Errors)
            );

            return result;
        }
               
        public async Task<IActionResult> StartPaymentFlatMap(StartPaymentRequest request)
        {
            var validation = _startPaymentValidator.Validate(request);
            var apiKey = validation.FlatMap((_) => GetApiKeyFromHeader());
            var appCodeOpt = apiKey.FlatMap(ak => _applicationProvider.GetClientApplicationCode(ak));  // tutaj nie był dozwolony zwrot "string" musiał być "Option<string>"

            // Przymusowe rozpakowanie:

            if (!appCodeOpt.HasValue)
            {
                var ex = "";
                appCodeOpt.MapException((fp) => ex = fp.Errors);
                return BadRequest(ex);
            }

            var appCode = "";
            appCodeOpt.MatchSome(aco => appCode = aco);

            // Koniec - tutaj już można użyć async-await

            var startUrl = await _paymentDomainService.StartPaymentAsync(request, appCode);

            IActionResult result = startUrl.Match(
                some: (sUrl) => Ok(sUrl),
                none: (fp) => BadRequest(fp.Errors)
            );

            return result;
        }

        #region Dependencies

        private Func<Option<string, FailPath>> GetApiKeyFromHeader;

        public class StartPaymentRequest { }

        public interface IStartPaymentValidator
        {
            Option<object, FailPath> Validate(StartPaymentRequest request);
        }

        public interface IApplicationProvider
        {
            Option<string, FailPath> GetClientApplicationCode(string apiKey);
        }

        public interface IPaymentDomainService
        {
            Task<Option<string, FailPath>> StartPaymentAsync(StartPaymentRequest request, string appCode);
        }

        private IActionResult BadRequest(string message = "")
        {
            return new BadResult(message);
        }

        private IActionResult Ok(string result)
        {
            return new OkResult();
        }

        public class FailPath
        {
            public string Errors;

            public FailPath(string errors)
            {
                this.Errors = errors;
            }
        }

        public interface IActionResult { }
        public class BadResult : IActionResult {
            public readonly string message;

            public BadResult(string message)
            {
                this.message = message;
            }
        }
        public class OkResult : IActionResult { }

        #endregion
    }
}
