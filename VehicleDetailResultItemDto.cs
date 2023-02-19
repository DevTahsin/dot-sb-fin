using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockbridgeFinancial.Crawler
{
    internal class VehicleDetailResultItemDto
    {
        public string Link { get; set; }
        public List<string> Images { get; set; }
        public string Contact { get; set; }
        public string StockType { get; set; }
        public string Title { get; set; }
        public string MileAge { get; set; }
        public string PrimaryPrice { get; set; }
        public string PriceDrop { get; set; }
        public string EstimatedMonthlyPayment { get; set; }
        public List<string> VehicleBadges { get; set; }
        public Dictionary<string, string> BasicData { get; set; }
        public Dictionary<string, string> FeaturesData { get; set; }
        public Dictionary<string, string> PriceHistory { get; set; }
        public string SellerName { get; set; }
        public string SellerRate { get; set; }
        public string SellerAddress { get; set; }
        public string SellerNoteAboutCar { get; set; }
        public string ConsumerReviewRating { get; set; }
        public string ConsumerReviewCount { get; set; }
        public Dictionary<string, string> ConsumerReviewRatingBreakdown { get; set; }
        public List<VehicleDetailResultItemConsumerReviewDto> ConsumerReviews { get; set; }
    }
    internal class VehicleDetailResultItemConsumerReviewDto
    {
        public string Rating { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string By { get; set; }
        public string Description { get; set; }
    }
}
