namespace VideoSyncer.Models
{
    /// <summary>
    /// 動画配置設定クラス
    /// </summary>
    public class GridPlacement
    {
        /// <summary>
        /// X座標
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y座標
        /// </summary>
        public int Y { get; set; }


        /// <summary>
        /// 動画数と出力動画サイズから動画配置を計算する
        /// </summary>
        /// <param name="count">動画数</param>
        /// <param name="targetWidth">出力動画幅</param>
        /// <param name="targetHeight">出力動画高</param>
        /// <returns>動画出力設定</returns>
        public static GridConfig GetLayoutConfig(int count, int targetWidth, int targetHeight, int inputWidth, int inputHeight)
        {
            var bestRows = count;
            var maxArea = 0;
            var bestW = 0;
            var bestH = 0;

            for (var cols = 1; cols <= count; cols++)
            {
                var rows = (int)Math.Ceiling((double)count / cols);
                var cellMaxW = targetWidth / cols;
                var cellMaxH = targetHeight / rows;

                var (w, h) = CalculateFittedSize(inputWidth, inputHeight, cellMaxW, cellMaxH);
                var area = w * h;

                if (area > maxArea)
                {
                    maxArea = area;
                    bestRows = rows;
                    bestW = w;
                    bestH = h;
                }
            }

            var rowCounts = CalculateBarrelRowCounts(count, bestRows);
            var placements = CalculatePlacements(bestRows, rowCounts, bestW, bestH);

            return new GridConfig
            {
                VideoWidth = bestW,
                VideoHeight = bestH,
                Placements = placements
            };
        }

        /// <summary>
        /// 入力動画のアスペクト比を維持したまま、指定した最大幅・高さに収まるサイズを計算する
        /// </summary>
        /// <param name="inputWidth">入力動画幅</param>
        /// <param name="inputHeight">入力動画高</param>
        /// <param name="maxW">1マスの最大幅</param>
        /// <param name="maxH">1マスの最大高さ</param>
        /// <returns>2の倍数に丸めた（幅, 高さ）</returns>
        private static (int Width, int Height) CalculateFittedSize(int inputWidth, int inputHeight, int maxW, int maxH)
        {
            var inputAspect = (double)inputWidth / inputHeight;
            var cellAspect = (double)maxW / maxH;

            var isWide = inputAspect > cellAspect;
            var w = RoundDownToEven(isWide ? maxW : maxH * inputAspect);
            var h = RoundDownToEven(isWide ? maxW / inputAspect : maxH);

            return (w, h);
        }

        /// <summary>
        /// 各行の配置数を計算する
        /// </summary>
        /// <param name="count">動画総数</param>
        /// <param name="rows">行数</param>
        /// <returns>各行の配置数</returns>
        private static int[] CalculateBarrelRowCounts(int count, int rows)
        {
            var rowCounts = new int[rows];
            var baseCount = count / rows;
            var remainder = count % rows;

            for (var i = 0; i < rows; i++)
            {
                rowCounts[i] = baseCount;
            }

            if (remainder <= 0)
            {
                return rowCounts;
            }

            var sortedPriorities = Enumerable.Range(0, rows)
                .Select(i => new { Index = i, Distance = Math.Abs(i - ((rows - 1) / 2.0)) })
                .OrderBy(x => x.Distance)
                .ThenByDescending(x => x.Index)
                .ToList();

            for (var i = 0; i < remainder; i++)
            {
                rowCounts[sortedPriorities[i].Index]++;
            }
            return rowCounts;
        }

        /// <summary>
        /// 指定した行数と各行の要素数から各動画の配置座標を計算する
        /// </summary>
        /// <param name="rows">行数</param>
        /// <param name="rowCounts">各行の要素数</param>
        /// <param name="baseW">各動画幅</param>
        /// <param name="baseH">各動画高さ</param>
        /// <returns>各動画の配置座標</returns>
        private static List<GridPlacement> CalculatePlacements(int rows, int[] rowCounts, int baseW, int baseH)
        {
            var placements = new List<GridPlacement>();
            var maxColsInRow = rowCounts.Max();
            var gridBlockWidth = maxColsInRow * baseW;

            for (var r = 0; r < rows; r++)
            {
                var itemsInRow = rowCounts[r];
                var rowWidth = itemsInRow * baseW;
                var startX = RoundDownToEven((gridBlockWidth - rowWidth) / 2);

                for (var c = 0; c < itemsInRow; c++)
                {
                    placements.Add(new GridPlacement
                    {
                        X = startX + (c * baseW),
                        Y = r * baseH
                    });
                }
            }
            return placements;
        }

        /// <summary>
        /// 値を偶数に切り捨てる
        /// </summary>
        /// <param name="value">丸める値</param>
        /// <returns>偶数に切り捨てた値</returns>
        private static int RoundDownToEven(double value) => (int)(value / 2) * 2;
    }
}