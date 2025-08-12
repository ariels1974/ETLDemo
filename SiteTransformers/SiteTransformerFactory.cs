using SiteTransformers.Transformers;

namespace SiteTransformers;

public class SiteTransformerFactory
{
    public ISiteTransformer GetTransformer(string site)
    {
        return site switch
        {
            "Payngo-Electric Scooter" => new PayngoTransformer(),
            "ALM-Electric Scooter" => new ALMTransformer(),
            _ => new DefaultTransformer(),
        };
    }
}