using Newtonsoft.Json.Linq;

namespace Stock.Data
{
    /// <summary>
    /// 股票资讯
    /// </summary>
    public class StockInfo
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// 开盘
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// 收盘
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// 涨幅
        /// </summary>
        public decimal Change { get; set; }

        /// <summary>
        /// 涨幅(%)
        /// </summary>
        public decimal ChangePercentage { get; set; }

        /// <summary>
        /// 最低
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// 最高
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// 成交量
        /// </summary>
        public int Volume { get; set; }

        /// <summary>
        /// 成交额
        /// </summary>
        public decimal Turnover { get; set; }

        /// <summary>
        /// 量比(%)
        /// </summary>
        public decimal VolumeRatio { get; set; }

        /// <summary>
        /// 获取股票历史信息
        /// </summary>
        /// <param name="code">股票代码</param>
        /// <param name="begin">开始日期</param>
        /// <param name="end">结束日期</param>
        public static async Task<List<StockInfo>> GetAsync(string code, DateTime begin, DateTime end)
        {
            var http = new HttpClient();
            var json = await http.GetStringAsync($"https://q.stock.sohu.com/hisHq?code=cn_{code}&start={begin:yyyyMMdd}&end={end:yyyyMMdd}");
            var stockData = JArray.Parse(json);
            var stockInfos = new List<StockInfo>();

            foreach (JObject stockInfoObj in stockData)
            {
                var stockInfoArray = (JArray)stockInfoObj["hq"]!;

                foreach (JArray info in stockInfoArray)
                {
                    var stockInfo = new StockInfo
                    {
                        Date = DateOnly.Parse((string)info[0]!),
                        Open = decimal.Parse((string)info[1]!),
                        Close = decimal.Parse((string)info[2]!),
                        Change = decimal.Parse((string)info[3]!),
                        ChangePercentage = decimal.Parse(((string)info[4]!).TrimEnd('%')),
                        Low = decimal.Parse((string)info[5]!),
                        High = decimal.Parse((string)info[6]!),
                        Volume = int.Parse((string)info[7]!),
                        Turnover = decimal.Parse((string)info[8]!),
                        VolumeRatio = decimal.Parse(((string)info[9]!).TrimEnd('%'))
                    };

                    stockInfos.Add(stockInfo);
                }
            }

            return stockInfos.OrderBy(stock=>stock.Date).ToList();
        }
    }
}
