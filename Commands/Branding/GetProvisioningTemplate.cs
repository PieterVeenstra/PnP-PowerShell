﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Framework.Provisioning.Connectors;
using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.ObjectHandlers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers;
using OfficeDevPnP.PowerShell.CmdletHelpAttributes;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using OfficeDevPnP.Core.Utilities;
using OfficeDevPnP.PowerShell.Commands.Enums;
using File = System.IO.File;
using Resources = OfficeDevPnP.PowerShell.Commands.Properties.Resources;


namespace OfficeDevPnP.PowerShell.Commands.Branding
{
    [Cmdlet(VerbsCommon.Get, "SPOProvisioningTemplate", SupportsShouldProcess = true)]
    [CmdletHelp("Generates a provisioning template from a web",
        Category = CmdletHelpCategory.Branding)]
    [CmdletExample(
       Code = @"PS:> Get-SPOProvisioningTemplate -Out template.xml",
       Remarks = "Extracts a provisioning template in XML format from the current web.",
       SortOrder = 1)]
    [CmdletExample(
        Code = @"PS:> Get-SPOProvisioningTemplate -Out template.xml -Schema V201503",
        Remarks = "Extracts a provisioning template in XML format from the current web and saves it in the V201503 version of the schema.",
        SortOrder = 2)]
    [CmdletExample(
        Code = @"PS:> Get-SPOProvisioningTemplate -Out template.xml -IncludeAllTermGroups",
        Remarks = "Extracts a provisioning template in XML format from the current web and includes all term groups, term sets and terms from the Managed Metadata Service Taxonomy.",
        SortOrder = 3)]
    [CmdletExample(
        Code = @"PS:> Get-SPOProvisioningTemplate -Out template.xml -IncludeSiteCollectionTermGroup",
        Remarks = "Extracts a provisioning template in XML format from the current web and includes the term group currently (if set) assigned to the site collection.",
        SortOrder = 4)]
    [CmdletExample(
        Code = @"PS:> Get-SPOProvisioningTemplate -Out template.xml -PersistComposedLookFiles",
        Remarks = "Extracts a provisioning template in XML format from the current web and saves the files that make up the composed look to the same folder as where the template is saved.",
        SortOrder = 5)]
    [CmdletExample(
        Code = @"PS:> Get-SPOProvisioningTemplate -Out template.xml -Handlers Lists, SiteSecurity",
        Remarks = "Extracts a provisioning template in XML format from the current web, but only processes lists and site security when generating the template.",
        SortOrder = 5)]

    public class GetProvisioningTemplate : SPOWebCmdlet
    {
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Filename to write to, optionally including full path")]
        public string Out;

        [Parameter(Mandatory = false, Position = 1, HelpMessage = "The schema of the output to use, defaults to the latest schema")]
        public XMLPnPSchemaVersion Schema = XMLPnPSchemaVersion.LATEST;

        [Parameter(Mandatory = false, HelpMessage = "If specified, all term groups will be included. Overrides IncludeSiteCollectionTermGroup.")]
        public SwitchParameter IncludeAllTermGroups;

        [Parameter(Mandatory = false, HelpMessage = "If specified, all the site collection term groups will be included. Overridden by IncludeAllTermGroups.")]
        public SwitchParameter IncludeSiteCollectionTermGroup;

        [Parameter(Mandatory = false, HelpMessage = "If specified all site groups will be included.")]
        public SwitchParameter IncludeSiteGroups;

        [Parameter(Mandatory = false, HelpMessage = "If specified the files used for masterpages, sitelogo, alternate CSS and the files that make up the composed look will be saved.")]
        public SwitchParameter PersistBrandingFiles;

        [Parameter(Mandatory = false, HelpMessage = "If specified the files making up the composed look (background image, font file and color file) will be saved.")]
        [Obsolete("Use PersistBrandingFiles instead.")]
        public SwitchParameter PersistComposedLookFiles;

        [Parameter(Mandatory = false, HelpMessage = "If specified the files used for the publishing feature will be saved.")]
        public SwitchParameter PersistPublishingFiles;

        [Parameter(Mandatory = false, HelpMessage = "If specified, out of the box / native publishing files will be saved.")]
        public SwitchParameter IncludeNativePublishingFiles;

        [Parameter(Mandatory = false, HelpMessage = "Allows you to only process a specific type of artifact in the site. Notice that this might result in a non-working template, as some of the handlers require other artifacts in place if they are not part of what your extracting.")]
        public Handlers Handlers;

        [Parameter(Mandatory = false, HelpMessage = "Allows you to run all handlers, excluding the ones specified.")]
        public Handlers ExcludeHandlers;

        [Parameter(Mandatory = false, HelpMessage = "Overwrites the output file if it exists.")]
        public SwitchParameter Force;

        [Parameter(Mandatory = false, HelpMessage = "Exports the template without the use of a base template, causing all OOTB artifacts to be included. Using this switch is generally not required/recommended.")]
        [Obsolete("Use of this method is generally not required/recommended")]
        public SwitchParameter NoBaseTemplate;

        [Parameter(Mandatory = false)]
        public System.Text.Encoding Encoding = System.Text.Encoding.Unicode;

     

        protected override void ExecuteCmdlet()
        {
            if (!string.IsNullOrEmpty(Out))
            {
                if (!Path.IsPathRooted(Out))
                {
                    Out = Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, Out);
                }
                if (File.Exists(Out))
                {
                    if (Force || ShouldContinue(string.Format(Resources.File0ExistsOverwrite, Out), Resources.Confirm))
                    {
                        var xml = GetProvisioningTemplateXML(Schema, new FileInfo(Out).DirectoryName);

                        File.WriteAllText(Out, xml, Encoding);
                    }
                }
                else
                {
                    var xml = GetProvisioningTemplateXML(Schema, new FileInfo(Out).DirectoryName);

                    File.WriteAllText(Out, xml, Encoding);
                }
            }
            else
            {
                var xml = GetProvisioningTemplateXML(Schema, SessionState.Path.CurrentFileSystemLocation.Path);

                WriteObject(xml);
            }
        }

        private string GetProvisioningTemplateXML(XMLPnPSchemaVersion schema, string path)
        {
            SelectedWeb.EnsureProperty(w => w.Url);

            var creationInformation = new ProvisioningTemplateCreationInformation(SelectedWeb);

            if (this.MyInvocation.BoundParameters.ContainsKey("Handlers"))
            {
                creationInformation.HandlersToProcess = Handlers;
            }
            if (this.MyInvocation.BoundParameters.ContainsKey("ExcludeHandlers"))
            {
                foreach (var handler in (OfficeDevPnP.Core.Framework.Provisioning.Model.Handlers[])Enum.GetValues(typeof(Handlers)))
                {
                    if (!ExcludeHandlers.Has(handler) && handler != Handlers.All)
                    {
                        Handlers = Handlers | handler;
                    }
                }
                creationInformation.HandlersToProcess = Handlers;
            }

            creationInformation.PersistBrandingFiles = PersistBrandingFiles || PersistComposedLookFiles;
            creationInformation.PersistPublishingFiles = PersistPublishingFiles;
            creationInformation.IncludeNativePublishingFiles = IncludeNativePublishingFiles;
            creationInformation.IncludeSiteGroups = IncludeSiteGroups;

            creationInformation.FileConnector = new FileSystemConnector(path, "");

#pragma warning disable CS0618 // Type or member is obsolete
            if (NoBaseTemplate)
            {
                creationInformation.BaseTemplate = null;
            }
            else
            {
                creationInformation.BaseTemplate = this.SelectedWeb.GetBaseTemplate();
            }
#pragma warning restore CS0618 // Type or member is obsolete

            creationInformation.ProgressDelegate = (message, step, total) =>
            {
                WriteProgress(new ProgressRecord(0, string.Format("Extracting Template from {0}", SelectedWeb.Url), message) { PercentComplete = (100 / total) * step });
            };
            creationInformation.MessagesDelegate = (message, type) =>
            {
                if (type == ProvisioningMessageType.Warning)
                {
                    WriteWarning(message);
                }
            };

            if (IncludeAllTermGroups)
            {
                creationInformation.IncludeAllTermGroups = true;
            }
            else
            {
                if (IncludeSiteCollectionTermGroup)
                {
                    creationInformation.IncludeSiteCollectionTermGroup = true;
                }
            }

            var template = SelectedWeb.GetProvisioningTemplate(creationInformation);

            ITemplateFormatter formatter = null;
            switch (schema)
            {
                case XMLPnPSchemaVersion.LATEST:
                    {
                        formatter = XMLPnPSchemaFormatter.LatestFormatter;
                        break;
                    }
                case XMLPnPSchemaVersion.V201503:
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        formatter = XMLPnPSchemaFormatter.GetSpecificFormatter(XMLConstants.PROVISIONING_SCHEMA_NAMESPACE_2015_03);
#pragma warning restore CS0618 // Type or member is obsolete
                        break;
                    }
                case XMLPnPSchemaVersion.V201505:
                    {
                        formatter = XMLPnPSchemaFormatter.GetSpecificFormatter(XMLConstants.PROVISIONING_SCHEMA_NAMESPACE_2015_05);
                        break;
                    }
                case XMLPnPSchemaVersion.V201508:
                    {
                        formatter = XMLPnPSchemaFormatter.GetSpecificFormatter(XMLConstants.PROVISIONING_SCHEMA_NAMESPACE_2015_08);
                        break;
                    }
                case XMLPnPSchemaVersion.V201512:
                    {
                        formatter = XMLPnPSchemaFormatter.GetSpecificFormatter(XMLConstants.PROVISIONING_SCHEMA_NAMESPACE_2015_12);
                        break;
                    }
            }
            var _outputStream = formatter.ToFormattedTemplate(template);
            StreamReader reader = new StreamReader(_outputStream);

            return reader.ReadToEnd();

        }
    }
}
