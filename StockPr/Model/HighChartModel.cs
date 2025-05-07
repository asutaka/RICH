namespace StockPr.Model
{
    public class HighChartModel
    {
        public HighChartModel(string input)
        {
            infile = input;
            width = false;
            scale = false;
            constr = "chart";
            type = "image/png";
            b64 = false;
        }
        public string infile { get; set; }
        public bool width { get; set; }
        public bool scale { get; set; }
        public string constr { get; set; }
        public string type { get; set; }
        public bool b64 { get; set; }
    }
    public class HighchartBasicColumn
    {
        public HighchartBasicColumn(string titl, List<string> lCat, List<HighChartSeries_BasicColumn> lSeries)
        {
            title = new HighChartTitle { text = titl };
            xAxis = new HighChartXAxis { categories = lCat };
            series = lSeries;
            credits = new HighChartCredits { enabled = false };
        }
        public HighChartTitle title { get; set; }
        public HighChartXAxis xAxis { get; set; }
        public List<HighChartYAxis> yAxis { get; set; }
        public List<HighChartSeries_BasicColumn> series { get; set; }
        public HighChartCredits credits { get; set; }
    }

    /// <summary>
    /// Gồm 1 cột nét và đứng phía sau là một cột mờ
    /// </summary>
    public class HighchartTangTruongTinDung
    {
        public HighchartTangTruongTinDung(string titl, List<string> lCat, List<HighChartSeries_TangTruongTinDung> lSeries)
        {
            title = new HighChartTitle { text = titl };
            xAxis = new HighChartXAxis { categories = lCat };
            series = lSeries;
            credits = new HighChartCredits { enabled = false };
            plotOptions = new HighChartPlotOptionsSimple { series = new HighChartPlotOptionsSeries { grouping = false } };
        }
        public HighChartTitle title { get; set; }
        public HighChartXAxis xAxis { get; set; }
        public List<HighChartYAxis> yAxis { get; set; }
        public List<HighChartSeries_TangTruongTinDung> series { get; set; }
        public HighChartCredits credits { get; set; }
        public HighChartPlotOptionsSimple plotOptions { get; set; }
    }

    /// <summary>
    /// Gồm biểu đồ cột với các giá trị xếp chồng lên nhau
    /// </summary>
    public class HighchartStack
    {
        public HighchartStack(string titl, List<string> lCat, List<HighChartSeries_BasicColumn> lSeries)
        {
            title = new HighChartTitle { text = titl };
            xAxis = new HighChartXAxis { categories = lCat };
            series = lSeries;
            credits = new HighChartCredits { enabled = false };
            plotOptions = new HighChartPlotOptions
            {
                column = new HighChartPlotOptionsColumn { stacking = "percent", dataLabels = new HighChartDataLabel { format = "{point.percentage:.0f}%" } },
                line = new HighChartPlotOptionsLine { dataLabels = new HighChartDataLabel { enabled = true } }
            };
        }
        public HighChartTitle title { get; set; }
        public HighChartXAxis xAxis { get; set; }
        public List<HighChartYAxis> yAxis { get; set; }
        public List<HighChartSeries_BasicColumn> series { get; set; }
        public HighChartCredits credits { get; set; }
        public HighChartPlotOptions plotOptions { get; set; }
    }
    /// <summary>
    /// Chart Thời Tiết
    /// </summary>
    public class HighChartTemperature
    {
        public HighChartTemperature(string titl, List<string> lCat, List<HighChartSeries_BasicColumnCustomColor> lSeries)
        {
            chart = new HighChartType { type = "columnrange" };
            title = new HighChartTitle { text = titl };
            xAxis = new HighChartXAxis { categories = lCat };
            yAxis = new List<HighChartYAxis> { new HighChartYAxis { title = new HighChartTitle { text = "" }, labels = new HighChartLabel { } } };
            plotOptions = new HighChartPlotOptions { columnrange = new HighChartPlotOptionColumnRange { negativeColor = "#F6465D" } };
            legend = new HighChartLegend { enabled = false };
            series = lSeries;
            credits = new HighChartCredits { enabled = false };
        }
        public HighChartType chart { get; set; }
        public HighChartTitle title { get; set; }
        public HighChartXAxis xAxis { get; set; }
        public List<HighChartYAxis> yAxis { get; set; }
        public HighChartPlotOptions plotOptions { get; set; }
        public HighChartLegend legend { get; set; }
        public List<HighChartSeries_BasicColumnCustomColor> series { get; set; }
        public HighChartCredits credits { get; set; }
    }


    public class HighChartBasicColumnCustomColor
    {
        public HighChartBasicColumnCustomColor(string titl, string titlSeries, List<List<object>> lSeries, bool isMixColor = true)
        {
            title = new HighChartTitle { text = titl };
            xAxis = new HighChartXAxis { type = "category" };
            yAxis = new HighChartYAxis { title = new HighChartTitle { text = string.Empty } };
            series = new List<HighChartSeries_BasicColumnCustomColor>
            {
                new HighChartSeries_BasicColumnCustomColor
                {
                    data = lSeries,
                    type = "column",
                    name = titlSeries,
                    colorByPoint = isMixColor,
                    colors = new List<string>
                    {
                        "#9b20d9", "#9215ac", "#861ec9", "#7a17e6", "#7010f9", "#691af3",
                        "#6225ed", "#5b30e7", "#533be1", "#4c46db", "#4551d5", "#3e5ccf",
                        "#3667c9", "#2f72c3", "#277dbd", "#1f88b7", "#1693b1", "#0a9eaa",
                        "#03c69b",  "#00f194"
                    }
                }
            };
            credits = new HighChartCredits { enabled = false };
        }
        public HighChartTitle title { get; set; }
        public HighChartXAxis xAxis { get; set; }
        public HighChartYAxis yAxis { get; set; }
        public List<HighChartSeries_BasicColumnCustomColor> series { get; set; }
        public HighChartCredits credits { get; set; }
    }

    #region ChienLuocDauTu
    public class HighChartChienLuocDauTu
    {
        public HighChartChienLuocDauTu(string titl, List<HighChartSeries_ChienLuocDauTu> lSeries)
        {
            title = new HighChartTitle { text = titl };
            series = lSeries;
            credits = new HighChartCredits { enabled = false };
        }
        public HighChartTitle title { get; set; }
        public List<HighChartSeries_ChienLuocDauTu> series { get; set; }
        public HighChartCredits credits { get; set; }
    }

    public class HighChartSeries_ChienLuocDauTu
    {
        public string type { get; set; }
        public List<HighChartSeriesData_ChienLuocDauTu> data { get; set; }
        public HighChartSeriesMarker_ChienLuocDauTu marker { get; set; }
        public int borderRadius { get; set; }
        public List<HighChartSeriesLevel_ChienLuocDauTu> levels { get; set; }
    }

    public class HighChartSeriesData_ChienLuocDauTu
    {
        public string id { get; set; }
        public string parent { get; set; }
        public string name { get; set; }
    }

    public class HighChartSeriesMarker_ChienLuocDauTu
    {
        public string symbol { get; set; }
        public string width { get; set; }
    }

    public class HighChartSeriesLevel_ChienLuocDauTu
    {
        public int level { get; set; }
        public bool levelIsConstant { get; set; }
        public bool colorByPoint { get; set; }
        public HighChartSeriesColor_ChienLuocDauTu colorVariation { get; set; }
    }

    public class HighChartSeriesColor_ChienLuocDauTu
    {
        public string key { get; set; }
        public double to { get; set; }
    }
    #endregion

    public class HighChartType
    {
        public string type { get; set; }
    }

    public class HighChartTitle
    {
        public string text { get; set; }
    }

    public class HighChartLabel
    {
        public string format { get; set; }
    }

    public class HighChartXAxis
    {
        public string type { get; set; }
        public IEnumerable<string> categories { get; set; }
    }

    public class HighChartYAxis
    {
        public HighChartTitle title { get; set; }
        public HighChartLabel labels { get; set; }
        public bool opposite { get; set; }
    }

    public class HighChartSeries
    {
        public string type { get; set; }
        public string name { get; set; }
        public int yAxis { get; set; }
        public IEnumerable<string> colors { get; set; }
        public string color { get; set; }
        public bool colorByPoint { get; set; }
        public HighChartDataLabel dataLabels { get; set; }
    }

    public class HighChartSeries_BasicColumn : HighChartSeries
    {
        public IEnumerable<double> data { get; set; }
    }

    public class HighChartSeries_BasicColumnCustomColor : HighChartSeries
    {
        public List<List<object>> data { get; set; }
    }

    public class HighChartSeries_TangTruongTinDung
    {
        public string name { get; set; }
        public string type { get; set; }
        public string color { get; set; }
        public double pointPlacement { get; set; }
        public List<double> data { get; set; }
        public HighChartDataLabel dataLabels { get; set; }
        public int yAxis { get; set; }
    }

    public class HighChartCredits
    {
        public bool enabled { get; set; }
    }

    public class HighChartPlotOptionsSimple
    {
        public HighChartPlotOptionsSeries series { get; set; }
    }

    public class HighChartPlotOptions
    {
        public HighChartPlotOptionsSeries series { get; set; }
        public HighChartPlotOptionsColumn column { get; set; }
        public HighChartPlotOptionsLine line { get; set; }
        public HighChartPlotOptionColumnRange columnrange { get; set; }
    }

    public class HighChartPlotOptionsSeries
    {
        public int borderWidth { get; set; }
        public HighChartDataLabel dataLabels { get; set; }
        public bool grouping { get; set; }
    }

    public class HighChartPlotOptionsColumn
    {
        public string stacking { get; set; }
        public HighChartDataLabel dataLabels { get; set; }
    }

    public class HighChartPlotOptionsLine
    {
        public HighChartDataLabel dataLabels { get; set; }
    }

    public class HighChartPlotOptionColumnRange
    {
        public string negativeColor { get; set; }
    }

    public class HighChartLegend
    {
        public bool enabled { get; set; }
    }

    public class HighChartDataLabel
    {
        public HighChartDataLabel()
        {
            enabled = true;
            style = new HighChartStyle();
        }
        public bool enabled { get; set; }
        public string format { get; set; }
        public HighChartStyle style { get; set; }
    }

    public class HighChartStyle
    {
        public HighChartStyle()
        {
            fontSize = "10px";
            fontFamily = "helvetica, arial, sans-serif";
            textShadow = false;
            fontWeight = "normal";
        }
        public string fontSize { get; set; }
        public string fontFamily { get; set; }
        public bool textShadow { get; set; }
        public string fontWeight { get; set; }
    }
}
