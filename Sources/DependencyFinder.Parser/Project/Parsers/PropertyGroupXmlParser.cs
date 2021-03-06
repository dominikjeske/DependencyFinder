﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ByteDev.DotNet.Project.Parsers
{
    internal static class PropertyGroupXmlParser
    {
        public static XElement GetOldStyleTargetElement(IEnumerable<XElement> propertyGroups)
        {
            const string name = "TargetFrameworkVersion";
            XNamespace nameSpace = "http://schemas.microsoft.com/developer/msbuild/2003";

            return propertyGroups.SingleOrDefault(pg => pg.Element(nameSpace + name) != null)?
                .Element(nameSpace + name);
        }

        public static XElement GetNewStyleTargetElement(IEnumerable<XElement> propertyGroups)
        {
            var singleTarget = propertyGroups.Descendants().FirstOrDefault(node => node.Name.LocalName == "TargetFramework");
            var multiTarget = propertyGroups.Descendants().FirstOrDefault(node => node.Name.LocalName == "TargetFrameworks");

            return singleTarget ?? multiTarget;
        }


    }
}