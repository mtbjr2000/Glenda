using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glenda
{
    public class CreditDto
    {
        public string AccountNumber { get; set; }
        public decimal Amount { get; set; }

        public override string ToString()
        {
            return String.Format("AcctNo: {0}, Amount = {1}", AccountNumber, Amount);
        }
    }
}
