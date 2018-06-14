using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace MyPSModule
{
    public class SqlConfigurationItem
    {
        public string Server { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public bool IsAdvanced { get; set; }
        public bool IsDynamic { get; set; }

        public int ConfigId { get; set; }
        public int ConfigValue { get; set; }
        public int RunValue { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }

    }
}
