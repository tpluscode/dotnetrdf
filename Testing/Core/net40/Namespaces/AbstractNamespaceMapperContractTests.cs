/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2013 dotNetRDF Project (dotnetrdf-develop@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Linq;
using NUnit.Framework;
using VDS.RDF.Nodes;

namespace VDS.RDF.Namespaces
{
    [TestFixture]
    public abstract class AbstractNamespaceMapperContractTests
    {
        /// <summary>
        /// Gets a new empty namespace mapper instance for testing
        /// </summary>
        /// <returns>New empty namespace mapper instance</returns>
        protected abstract INamespaceMapper GetInstance();

        [Test]
        public void NamespaceMapperContractAdd1()
        {
            INamespaceMapper nsmap = this.GetInstance();
            Assert.AreEqual(0, nsmap.Prefixes.Count());

            Uri u = new Uri("http://example.org/ns#");
            nsmap.AddNamespace("ex", u);
            Assert.AreEqual(1, nsmap.Prefixes.Count());
            Assert.IsTrue(nsmap.HasNamespace("ex"));
            Assert.IsTrue(EqualityHelper.AreUrisEqual(u, nsmap.GetNamespaceUri("ex")));
        }

        [Test]
        public void NamespaceMapperContractAdd2()
        {
            INamespaceMapper nsmap = this.GetInstance();
            Assert.AreEqual(0, nsmap.Prefixes.Count());

            Uri u = new Uri("http://example.org/ns#");
            nsmap.AddNamespace("ex", u);
            Assert.AreEqual(1, nsmap.Prefixes.Count());
            Assert.IsTrue(nsmap.HasNamespace("ex"));
            Assert.IsTrue(EqualityHelper.AreUrisEqual(u, nsmap.GetNamespaceUri("ex")));

            // Add second namespace
            u = new Uri("http://example.org/some/path/");
            nsmap.AddNamespace("eg", u);
            Assert.AreEqual(2, nsmap.Prefixes.Count());
            Assert.IsTrue(nsmap.HasNamespace("eg"));
            Assert.IsTrue(EqualityHelper.AreUrisEqual(u, nsmap.GetNamespaceUri("eg")));
        }

        [Test]
        public void NamespaceMapperContractAdd3()
        {
            INamespaceMapper nsmap = this.GetInstance();
            Assert.AreEqual(0, nsmap.Prefixes.Count());

            Uri u1 = new Uri("http://example.org/ns#");
            nsmap.AddNamespace("ex", u1);
            Assert.AreEqual(1, nsmap.Prefixes.Count());
            Assert.IsTrue(nsmap.HasNamespace("ex"));
            Assert.IsTrue(EqualityHelper.AreUrisEqual(u1, nsmap.GetNamespaceUri("ex")));

            // Overwrite namespace
            Uri u2 = new Uri("http://example.org/some/path/");
            nsmap.AddNamespace("ex", u2);
            Assert.AreEqual(1, nsmap.Prefixes.Count());
            Assert.IsTrue(nsmap.HasNamespace("ex"));
            Assert.IsTrue(EqualityHelper.AreUrisEqual(u2, nsmap.GetNamespaceUri("ex")));
            Assert.IsFalse(EqualityHelper.AreUrisEqual(u1, nsmap.GetNamespaceUri("ex")));
        }

        [Test, ExpectedException(typeof(RdfException))]
        public void NamespaceMapperContractAddBad1()
        {
            INamespaceMapper nsmap = this.GetInstance();
            Assert.AreEqual(0, nsmap.Prefixes.Count());

            // Relative namespace URIs are forbidden
            Uri u = new Uri("file.ext", UriKind.Relative);
            nsmap.AddNamespace("ex", u);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void NamespaceMapperContractAddBad2()
        {
            INamespaceMapper nsmap = this.GetInstance();
            Assert.AreEqual(0, nsmap.Prefixes.Count());

            // Null namespace URIs are forbidden
            nsmap.AddNamespace("ex", null);
        }
    }

    [TestFixture]
    public class NamespaceMapperContractTests
        : AbstractNamespaceMapperContractTests
    {
        protected override INamespaceMapper GetInstance()
        {
            return new NamespaceMapper(true);
        }
    }

    [TestFixture]
    public class NestedNamespaceMapperContractTests
        : AbstractNamespaceMapperContractTests
    {
        protected override INamespaceMapper GetInstance()
        {
            return new NestedNamespaceMapper(true);
        }
    }
}
