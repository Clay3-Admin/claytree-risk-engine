using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Claytree.Risk.Functions.Options
{
    public sealed class ApplicationNumberOptions
    {
        public bool Enabled { get; set; } = true;
        public string DefaultFormat { get; set; } = "{BRANCH:3}-{PRODUCT:2}-{YY}{MM}-{SERIAL:6}";
        public string Mode { get; set; } = "SERIAL"; // SERIAL | RANDOM
        public string DefaultBranchCode { get; set; } = "MUM";
        public string DefaultProductCode { get; set; } = "PL";
    }
}
