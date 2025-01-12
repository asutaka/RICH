namespace StockPr.Model
{
    public class BaseResponse<T>
    {
        public List<T> data { get; set; }
    }
    public class ReportDataIDResponse : BaseResponse<ReportDataIDDetailResponse> { }

    public class ReportDataIDDetailResponse
    {
        public int RowNumber { get; set; }
        public int ReportDataID { get; set; }
        public int YearPeriod { get; set; }
        public int ReportTermID { get; set; }
        public int Isunited { get; set; }
        public int BasePeriodBegin { get; set; }
    }


    public class ReportTempIDResponse : BaseResponse<ReportTempIDDetailResponse> { }
    public class ReportTempIDDetailResponse
    {
        public int RowNumber { get; set; }
        public string IdTemp { get; set; }
        public int YearPeriod { get; set; }
        public int ReportTermID { get; set; }
    }


    public class ReportDataDetailValue_BCTTResponse : BaseResponse<ReportDataDetailValue_BCTTDetailResponse> { }

    public class ReportDataDetailValue_BCTTDetailResponse
    {
        public int ReportnormId { get; set; }
        public double? Value1 { get; set; }
        public double? Value2 { get; set; }
        public double? Value3 { get; set; }
        public double? Value4 { get; set; }
        public double? Value5 { get; set; }
        public double? Value6 { get; set; }
        public double? Value7 { get; set; }
        public double? Value8 { get; set; }
        public double? Value9 { get; set; }
    }

    public class TempDetailValue_CSTCResponse : BaseResponse<TempDetailValue_CSTCDetailResponse> { }

    public class TempDetailValue_CSTCDetailResponse
    {
        public int FinanceIndexID { get; set; }
        public double? Value1 { get; set; }
        public double? Value2 { get; set; }
        public double? Value3 { get; set; }
        public double? Value4 { get; set; }
        public double? Value5 { get; set; }
        public double? Value6 { get; set; }
        public double? Value7 { get; set; }
        public double? Value8 { get; set; }
        public double? Value9 { get; set; }
    }

    public class BCTCAPIResponse
    {
        public string FileExt { get; set; }
        public int TotalRow { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string FullName { get; set; }
        public string LastUpdate { get; set; }
    }
}
