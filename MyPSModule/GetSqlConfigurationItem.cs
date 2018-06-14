using System.Collections.Generic;
using System.Management.Automation;
using System.Security;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Reflection;


namespace MyPSModule
{
    [CmdletBinding()]
    [Cmdlet(VerbsCommon.Get, "SqlConfigurationItem")]
    public class GetSqlConfigurationItem : Cmdlet, IDynamicParameters
    {
        [Parameter(Mandatory = true, Position = 1)]
        [Alias("ServerInstance")]
        public string SqlInstance { get; set; }

        [Parameter(Mandatory = false)]
        public string UserName { get; set; }
        [Parameter(Mandatory = false)]
        public string Password { get; set; }

        [Parameter]
        public SecureString SecurePassword { get; set; }

        private RuntimeDefinedParameterDictionary _configurationItem;

        [Parameter(Mandatory = false)]
        public PSCredential Credential { get; set; }

        public object GetDynamicParameters()
        {
            if (SqlInstance != null)
            {
                _configurationItem = ConfigurationItemArrayDynamicParameter.Attributes(SqlInstance);
                return _configurationItem;
            }
            else
            {
                return null;
            }
        }

        protected override void ProcessRecord()
        {
            List<ConfigProperty> items;

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
            items = new List<ConfigProperty>();

            foreach (var name in (string[])_configurationItem["ConfigurationItem"].Value)
            {
                if (server.Configuration.Properties[name] != null)
                {
                    server.Configuration.Refresh();
                    var prop = server.Configuration.Properties[name];
                    items.Add(prop);
                }
            }

            WriteObject(items.ToArray(), true);
        }
    }
    public class ConfigurationItemArrayDynamicParameter
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
            RuntimeDefinedParameter dynamicParam = new RuntimeDefinedParameter("ConfigurationItem", typeof(string[]), attributeCollection);
            RuntimeDefinedParameterDictionary paramDict = new RuntimeDefinedParameterDictionary();

            paramDict.Add("ConfigurationItem", dynamicParam);

            return paramDict;
        }
        public string[] ConfigurationItem { get; set; }
    }
}

