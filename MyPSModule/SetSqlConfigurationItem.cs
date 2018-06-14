using System.Collections.Generic;
using System.Management.Automation;
using System.Security;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Reflection;
using System.Collections.ObjectModel;
using System;

namespace MyPSModule
{
    [CmdletBinding()]
    [Cmdlet(VerbsCommon.Set, "SqlConfigurationItem")]
    public class SetSqlConfigurationitem : Cmdlet, IDynamicParameters
    {
        [Parameter(Mandatory = true, Position = 1)]
        [Alias("ServerInstance")]
        public string SqlInstance { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        public int Value { get; set; }

        [Parameter(Mandatory = false)]
        public string UserName { get; set; }
        [Parameter(Mandatory = false)]
        public string Password { get; set; }

        [Parameter]
        public SecureString SecurePassword { get; set; }

        private RuntimeDefinedParameterDictionary _configurationItem;

        [Parameter(Mandatory = false)]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter Force
        {
            get { return force; }
            set { force = value; }
        }
        private bool force;

        public object GetDynamicParameters()
        {
            if (SqlInstance != null)
            {
                _configurationItem = ConfigurationItemDynamicParameter.Attributes(SqlInstance);
                return _configurationItem;
            }
            else
            {
                return null;
            }
        }

        protected override void ProcessRecord()
        {
            ServerConnection connection = new ServerConnection
            {
                DatabaseName = "master",
                ServerInstance = SqlInstance
            };

            if (Credential != null)
            {
                connection.ConnectAsUserName = Credential.UserName;
                connection.SecurePassword = Credential.Password;
            }
            else if (UserName != null && (Password != null || SecurePassword != null))
            {
                connection.ConnectAsUserName = UserName;

                if (Password != null)
                {
                    connection.Password = Password;
                }
                else if (SecurePassword != null)
                {
                    connection.SecurePassword = SecurePassword;
                }
            }

            Server server = new Server(connection);

            var item = (string)_configurationItem["ConfigurationItem"].Value;

            if (server.Configuration.Properties[item] != null)
            {
                server.Configuration.Refresh();
                var prop = server.Configuration.Properties[item];
                if (Value > prop.Maximum || Value < prop.Minimum)
                {
                    WriteError(new ErrorRecord(new InvalidArgumentException(string.Format("Property {0} has a minimum of {1} and a maximum of {2}. Resubmit with a value between the Min and Max Values", item, prop.Minimum, prop.Maximum)), "ArgumentNotFound", ErrorCategory.InvalidArgument, (object)item));
                }
                else
                {
                    prop.ConfigValue = Value;
                    if (force)
                    {
                        server.Configuration.Alter(true);

                    }
                    else
                    {
                        server.Configuration.Alter();
                    }
                }
            }
            else
            {
                WriteError(new ErrorRecord(new InvalidArgumentException(string.Format("Property {0} is not available in this version of SQL Server", item)), "ArgumentNotFound", ErrorCategory.InvalidArgument, (object)item));
            }
            server.ConnectionContext.Disconnect();
        }
    }

    public class ConfigurationItemDynamicParameter
    {
        public static RuntimeDefinedParameterDictionary Attributes(string SqlInstance)
        {
            List<string> sourceList = new List<string>();
            Server server = new Server(SqlInstance);
            PropertyInfo[] props = server.Configuration.GetType().GetProperties();

            foreach (PropertyInfo prop in props)
            {
                sourceList.Add(prop.Name);
            }

            string[] sources = sourceList.ToArray();

            ParameterAttribute paramAttribute = new ParameterAttribute()
            {
                Mandatory = true,
                ValueFromPipelineByPropertyName = false
            };

            System.Collections.ObjectModel.Collection<System.Attribute> attributeCollection = new System.Collections.ObjectModel.Collection<System.Attribute>();
            paramAttribute.HelpMessage = "This is the help message.";
            attributeCollection.Add(paramAttribute);

            ValidateSetAttribute sourceFromConfig = new ValidateSetAttribute(sources);
            attributeCollection.Add(sourceFromConfig);
            RuntimeDefinedParameter dynamicParam = new RuntimeDefinedParameter("ConfigurationItem", typeof(string), attributeCollection);
            RuntimeDefinedParameterDictionary paramDict = new RuntimeDefinedParameterDictionary();

            paramDict.Add("ConfigurationItem", dynamicParam);

            return paramDict;
        }
        public string ConfigurationItem { get; set; }
    }
}