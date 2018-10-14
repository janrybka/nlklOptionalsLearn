using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Optional;
using FluentAssertions;

namespace BC.NPP.Nlkl.Optional.Tests
{
    [TestFixture]
    public class NlklTests
    {
        [Test]
        public void WhatIsNone1()
        {
            var stringNull = Option.Some<string>(null);
            var none = Option.None<string>();

            stringNull.Should().NotBe(none);
        }

        [Test]
        public void WhatIsNone1a()
        {
            var stringNull = OptionExtensions.SomeNotNull<string>(null);
            var none = Option.None<string>();

            stringNull.Should().Be(none);
        }

        [Test]
        public void WhatIsNone1b()
        {
            int? intNull = null;
            var intNullOption = intNull.ToOption();
            var none = Option.None<int>();

            intNullOption.Should().Be(none);
        }

        [Test]
        public void WhatIsNone2()
        {
            var stringNull = OptionExtensions.SomeNotNull<string>(null);
            var none = Option.Some<string>("");

            stringNull.Should().NotBe(none);
        }

        [Test]
        public void WhatIsNone3()
        {
            var empty1 = Option.Some<string>("");
            var empty2 = Option.Some<string>("");

            empty1.Should().Be(empty2);
        }
        
        [Test]
        public void WhatIsNone4()
        {
            var empty1 = Option.Some<string>(null);
            var empty2 = Option.Some<string>(null);

            empty1.Should().Be(empty2);
        }

        [Test]
        public void WhatIsNone5()
        {
            var empty1 = OptionExtensions.SomeNotNull<string>(null);
            var empty2 = OptionExtensions.SomeNotNull<string>(null);

            empty1.Should().Be(empty2);
        }

        [Test]
        public void WhatIsNone6()
        {
            var none1 = Option.None<string>();
            var none2 = Option.None<string>();

            none1.Should().Be(none2);
        }

        [Test]
        public void WhatIsObjectNone1()
        {
            var objNull = OptionExtensions.SomeNotNull<SomeClass>(null);
            var none = Option.None<SomeClass>();

            objNull.Should().Be(none);
        }

        [Test]
        public void WhatIsObjectNone2()
        {
            var obj = Option.Some<SomeClass>(new SomeClass());
            var none = Option.None<SomeClass>();

            obj.Should().NotBe(none);
        }

        private class SomeClass { }
    }
}
