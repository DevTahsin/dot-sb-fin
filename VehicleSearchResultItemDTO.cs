using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockbridgeFinancial.Crawler
{
    internal class VehicleSearchResultItemDTO
    {
        public string StockType { get; set; }
        public string DetailLink { get; set; }
        public string Title { get; set; }
        public string MileAge { get; set; }
        public string PrimaryPrice { get; set; }
        public string SecondaryPrice { get; set; }
        public string EstimatedMonthlyPayment { get; set; }
        public string DealerName { get; set; }
        public string SdsRatingCount { get; set; }
        public string SdsRatingReviewCount { get; set; }
        public List<string> Images { get; set; }

    }
}
