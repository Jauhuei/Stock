namespace Stock.Data
{
    /// <summary>
    /// 资产类
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// 初始金额
        /// </summary>
        public decimal Initail { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateOnly Date => StockInfo.Date;

        /// <summary>
        /// 总资产
        /// </summary>
        public decimal TotalAsset => Cash + SharesHeld * StockInfo.Close;

        /// <summary>
        /// 现金占比(%)
        /// </summary>
        public decimal CashRate => Math.Floor(Cash * 10000 / TotalAsset) / 100;

        /// <summary>
        /// 股票占比(%)
        /// </summary>
        public decimal SharesRate => Math.Floor(SharesHeld * StockInfo.Close * 10000 / TotalAsset) / 100;

        /// <summary>
        /// 收益率(%)
        /// </summary>
        public decimal Income => Math.Floor((TotalAsset - Initail) * 10000 / Initail) / 100;

        /// <summary>
        /// 收益对比(%)
        /// </summary>
        public decimal? IncomeComparison
        {
            get
            {
                if (FirstStockInfo.Date >= Date) return null;
                return Math.Floor((((TotalAsset - Initail) / Initail - (StockInfo.Close - FirstStockInfo.Close) / FirstStockInfo.Close)) * 10000) / 100;
            }
        }

        /// <summary>
        /// 可用现金
        /// </summary>
        public decimal Cash { get; set; }

        /// <summary>
        /// 持股数
        /// </summary>
        public int SharesHeld { get; set; }

        /// <summary>
        /// 股票资讯
        /// </summary>
        public StockInfo StockInfo { get; set; } = default!;

        /// <summary>
        /// 首日股票资讯
        /// </summary>
        public StockInfo FirstStockInfo { get; set; } = default!;

        /// <summary>
        /// 交易细节
        /// </summary>
        public List<string> TradeInfos { get; set; } = new List<string>();

        /// <summary>
        /// 股票回测
        /// </summary>
        /// <param name="cash">本金</param>
        /// <param name="stockInfos">股票资讯</param>
        public static List<Asset> Backtest(decimal cash, List<StockInfo> stockInfos)
        {
            var assets = new List<Asset>();

            for (int i = 0; i < 30; i++)
            {
                assets.Add(new Asset
                {
                    Initail = cash,
                    Cash = cash,
                    StockInfo = stockInfos[i],
                    FirstStockInfo = stockInfos[30]
                });
            }

            Backtest(assets, stockInfos.Skip(30));
            return assets;
        }

        /// <summary>
        /// 股票回测
        /// </summary>
        /// <param name="assets">资产</param>
        /// <param name="stockInfos">股票资讯</param>
        public static void Backtest(List<Asset> assets, IEnumerable<StockInfo> stockInfos)
        {
            var yesterday = assets.Last();
            Asset? today = null;

            // 复制资产
            if (!stockInfos.Any())
            {
                var stock = new StockInfo()
                {
                    Date = assets.Last().Date.AddDays(1),
                    Close = assets.Last().StockInfo.Close,
                    Open = assets.Last().StockInfo.Open,
                    Low = assets.Last().StockInfo.Low,
                    High = assets.Last().StockInfo.High,
                };
                today = yesterday.Clone(stock);
            }
            else
            {
                today = yesterday.Clone(stockInfos.First());
            }

            // 制定交易方法
            var last = assets.TakeLast(30).ToList();
            last.AddRange(assets.TakeLast(15));
            last.AddRange(assets.TakeLast(10));
            last.AddRange(assets.TakeLast(5));
            last.AddRange(assets.TakeLast(4));
            last.AddRange(assets.TakeLast(3));
            last.AddRange(assets.TakeLast(2));
            last.AddRange(assets.TakeLast(1));
            // 计划卖出价格
            var preSellPrice = Math.Floor(last.Average(every => every.StockInfo.Close) * 1.06m * 100) / 100;
            // 计划买入价格
            var predictBuyPrice = Math.Floor(last.Average(every => every.StockInfo.Close) * 1.0m * 100) / 100;
            // 计划卖出数量(股)
            var predictSellCount = (int)Math.Floor(yesterday.SharesHeld / 100 * 1.0m) * 100;
            // 计划买入数量(股)
            var predictBuyCount = (int)Math.Floor(today.Cash / 100 / yesterday.StockInfo.Close * 0.5m) * 100;

            // 避免买入量太少
            if (predictBuyCount * predictBuyPrice < 10000) predictBuyCount = 0;

            if (!stockInfos.Any())
            {
                today.TradeInfos.Add("下一个交易日");
                today.TradeInfos.Add($"挂卖 {predictSellCount} * {preSellPrice}");
                today.TradeInfos.Add($"挂买 {predictBuyCount} * {predictBuyPrice}");
                assets.Add(today);

                return;
            }
            else
            {
                int sellCount = 0;
                int buyCount = 0;

                if (preSellPrice <= today.StockInfo.High)
                {
                    // 成功卖出
                    sellCount = predictSellCount;
                    if (sellCount > 0) today.TradeInfos.Add($"挂卖 {predictSellCount} * {preSellPrice} ，卖成 {sellCount} * {Math.Max(preSellPrice, today.StockInfo.Low)}");
                    else today.TradeInfos.Add($"挂卖 {predictSellCount} * {preSellPrice}");
                }
                else
                {
                    today.TradeInfos.Add($"挂卖 {predictSellCount} * {preSellPrice}");
                }

                if (predictBuyPrice >= today.StockInfo.Low)
                {
                    // 成功买入
                    buyCount = predictBuyCount;
                    if (buyCount > 0) today.TradeInfos.Add($"挂买 {predictBuyCount} * {predictBuyPrice} ，买成 {buyCount} * {Math.Min(predictBuyPrice, today.StockInfo.High)}");
                    else today.TradeInfos.Add($"挂买 {predictBuyCount} * {predictBuyPrice}");
                }
                else
                {
                    today.TradeInfos.Add($"挂买 {predictBuyCount} * {predictBuyPrice}");
                }

                today.SharesHeld = today.SharesHeld - sellCount + buyCount;
                today.Cash = today.Cash + sellCount * Math.Max(preSellPrice, today.StockInfo.Low) - buyCount * Math.Min(predictBuyPrice, today.StockInfo.High);

                assets.Add(today);
                Backtest(assets, stockInfos.Skip(1));
            }
        }

        /// <summary>
        /// 拷贝昨日资产情况
        /// </summary>
        /// <param name="today">今日股价资讯</param>
        private Asset Clone(StockInfo today)
        {
            return new Asset()
            {
                Initail = this.Initail,
                Cash = this.Cash,
                SharesHeld = this.SharesHeld,
                StockInfo = today,
                FirstStockInfo = this.FirstStockInfo,
            };
        }

        public override string ToString()
        {
            return $"[{StockInfo.Date}] {TotalAsset}({(TotalAsset - Initail) / Initail * 100:F2}%) = {Cash} + {SharesHeld}手 * {StockInfo.Close}";
        }
    }
}
