namespace StockPr.Model.BCPT
{
    public class DSC_Main
    {
        public DSC_PageProp pageProps { get; set; }
    }

    public class DSC_PageProp
    {
        public DSC_DataCategory dataCategory { get; set; }
    }

    public class DSC_DataCategory
    {
        public DSC_DataList dataList { get; set; }
    }

    public class DSC_DataList
    {
        public List<DSC_Data> data { get; set; }
    }
    public class DSC_Data
    {
        public int id { get; set; }
        public DSC_Atribute attributes { get; set; }
    }

    public class DSC_Atribute
    {
        public string title { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public DateTime publishedAt { get; set; }
        public DateTime public_at { get; set; }
        public string slug { get; set; }
        public DSC_Category category_id { get; set; }
    }

    public class DSC_Category
    {
        public DSC_Data data { get; set; }
    }


    public class VNDirect_Main
    {
        public List<VNDirect_Data> data { get; set; }
    }

    public class VNDirect_Data
    {
        public string newsId { get; set; }
        public string tagsCode { get; set; }
        public string newsTitle { get; set; }
        public string newsDate { get; set; }
        public string newsTime { get; set; }
        public List<VNDirect_Attachment> attachments { get; set; }
    }

    public class VNDirect_Attachment
    {
        public string name { get; set; }
    }


    public class MigrateAsset_Main
    {
        public List<MigrateAsset_Data> data { get; set; }
    }

    public class MigrateAsset_Data
    {
        public int id { get; set; }
        public string title { get; set; }
        public string file_path { get; set; }
        public DateTime published_at { get; set; }
        public string stock_related { get; set; }
    }


    public class AGR_Data
    {
        public int ReportID { get; set; }
        public string Symbol { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
    }


    public class VCI_Main
    {
        public VCI_Data data { get; set; }
    }

    public class VCI_Data
    {
        public VCI_Response pagingGeneralResponses { get; set; }
    }

    public class VCI_Response
    {
        public List<VCI_Content> content { get; set; }
    }

    public class VCI_Content
    {
        public int id { get; set; }
        public string name { get; set; }
        public string file { get; set; }
        public DateTime makerDate { get; set; }
        public string pageLink { get; set; }
    }


    public class VCBS_Main
    {
        public List<VCBS_Data> data { get; set; }
    }

    public class VCBS_Data
    {
        public int id { get; set; }
        public string stockSymbol { get; set; }
        public string name { get; set; }
        public VCBS_Category category { get; set; }
        public VCBS_File file { get; set; }
        public DateTime publishedAt { get; set; }
    }

    public class VCBS_Category
    {
        public string code { get; set; }
    }

    public class VCBS_File
    {
        public string name { get; set; }
    }

    public class BCPT_Crawl_Data
    {
        public string id { get; set; }
        public string title { get; set; }
        public DateTime date { get; set; }
        public string path { get; set; }
    }

    public class BaoCaoPhanTichModel
    {
        public string content { get; set; }
        public string link { get; set; }
    }

}
