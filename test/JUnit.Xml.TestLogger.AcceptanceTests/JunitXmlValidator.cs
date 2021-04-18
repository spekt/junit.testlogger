// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JUnit.Xml.TestLogger.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;

    public class JunitXmlValidator
    {
        /// <summary>
        /// Field is provided only to simplify debugging test failures.
        /// </summary>
        private readonly List<XmlSchemaException> failures = new List<XmlSchemaException>();

        public bool IsValid(string xml)
        {
            var xmlReader = new StringReader(xml);
            var xsdReader = new StringReader(
                File.ReadAllText(
                    Path.Combine("..", "..", "..", "..", "assets", "JUnit.xsd")));

            var schema = XmlSchema.Read(
                xsdReader,
                (sender, args) => { throw new XmlSchemaValidationException(args.Message, args.Exception); });

            var xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add(schema);
            xmlReaderSettings.ValidationType = ValidationType.Schema;

            var veh = new ValidationEventHandler(this.XmlValidationEventHandler);

            xmlReaderSettings.ValidationEventHandler += veh;
            using (XmlReader reader = XmlReader.Create(xmlReader, xmlReaderSettings))
            {
                while (reader.Read())
                {
                }
            }

            xmlReaderSettings.ValidationEventHandler -= veh;

            return this.failures.Any() == false;
        }

        public bool IsValid(XDocument doc) => this.IsValid(doc.ToString());

        private void XmlValidationEventHandler(object sender, ValidationEventArgs e)
        {
            this.failures.Add(e.Exception);
        }
    }
}
