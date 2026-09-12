namespace CloudManager.Host.Mappers;

using CloudManager.Host.Models.Data;
using CloudManager.Host.Models.Forms;

using Smart.Mapper;

internal static partial class DataMapper
{
    [Mapper]
    public static partial DataForm ToForm(this DataEntity entity);

    [Mapper]
    public static partial DataResponse ToResponse(this DataEntity entity);
}
