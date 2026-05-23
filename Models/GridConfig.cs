namespace VideoSyncer.Models
{
    /// <summary>
    /// 動画出力設定クラス
    /// </summary>
    public class GridConfig
    {
        /// <summary>
        /// 動画幅
        /// </summary>
        public int VideoWidth { get; set; }
        /// <summary>
        /// 動画高
        /// </summary>
        public int VideoHeight { get; set; }

        /// <summary>
        /// 動画配置
        /// </summary>
        public required List<GridPlacement> Placements { get; set; }
    }
}