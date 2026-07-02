namespace CykModUtils.Game
{
    /// <summary>
    /// 对 GameUtil 常用格式化方法的短命名包装。
    /// </summary>
    public static class GameFormatUtility
    {
        /// <summary>
        /// 按玩家设置格式化质量。
        /// </summary>
        /// <param name="mass">质量，单位 kg。</param>
        /// <param name="timeSlice">是否追加每秒/每周期。</param>
        /// <returns>格式化后的质量文本。</returns>
        public static string Mass(float mass, GameUtil.TimeSlice timeSlice = GameUtil.TimeSlice.None)
        {
            return GameUtil.GetFormattedMass(mass, timeSlice);
        }

        /// <summary>
        /// 按玩家设置格式化温度。
        /// </summary>
        /// <param name="kelvin">温度，单位 K。</param>
        /// <param name="round">是否四舍五入。</param>
        /// <returns>格式化后的温度文本。</returns>
        public static string Temperature(float kelvin, bool round = false)
        {
            return GameUtil.GetFormattedTemperature(kelvin, GameUtil.TimeSlice.None, GameUtil.TemperatureInterpretation.Absolute, true, round);
        }

        /// <summary>
        /// 格式化周期数。
        /// </summary>
        /// <param name="cycles">周期数。</param>
        /// <param name="format">数字格式。</param>
        /// <param name="includeSuffix">是否包含周期单位。</param>
        /// <returns>格式化后的周期文本。</returns>
        public static string Cycles(float cycles, string format = "0.#", bool includeSuffix = true)
        {
            return GameUtil.GetFormattedCycles(cycles, format, includeSuffix);
        }

        /// <summary>
        /// 格式化功率。
        /// </summary>
        /// <param name="watts">瓦特数。</param>
        /// <param name="unit">显示单位。</param>
        /// <returns>格式化后的功率文本。</returns>
        public static string Wattage(float watts, GameUtil.WattageFormatterUnit unit = GameUtil.WattageFormatterUnit.Automatic)
        {
            return GameUtil.GetFormattedWattage(watts, unit);
        }

        /// <summary>
        /// 格式化百分比。
        /// </summary>
        /// <param name="percent">百分比数值，例如 50 表示 50%。</param>
        /// <param name="allowHundredths">小于 0.1 时是否允许显示百分位小数。</param>
        /// <returns>格式化后的百分比数字，不额外追加百分号。</returns>
        public static string Percent(float percent, bool allowHundredths = false)
        {
            return GameUtil.GetStandardPercentageFloat(percent, allowHundredths);
        }

        /// <summary>
        /// 格式化普通浮点数。
        /// </summary>
        /// <param name="value">数值。</param>
        /// <returns>符合 ONI UI 习惯的数字文本。</returns>
        public static string Number(float value)
        {
            return GameUtil.GetStandardFloat(value);
        }
    }
}
