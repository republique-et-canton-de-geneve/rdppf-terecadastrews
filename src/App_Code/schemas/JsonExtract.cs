/* $Rev: 19122 $ */
using ExtractData_v103;

namespace JsonExtract
{
    public class JsonExtract
    {
        public Extract Item { get; set; }
    }

    public class JsonEmbeddableExtract
    {
        public GetExtractByIdResponseTypeEmbeddable Item { get; set; }
    }

    public class JsonEGRID
    {
        public GetEGRIDResponseType Item { get; set; }
    }
}