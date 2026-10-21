namespace UniEvalUtsc.Web.Services;

public interface IPlantillasService
{
    Task<IReadOnlyList<PlantillaAdministrativa>> ObtenerPlantillasAsync(CancellationToken ct = default);

    Task<PlantillaAdministrativa> ObtenerAsync(string plantillaId, CancellationToken ct = default);

    Task<PlantillaAdministrativa> CrearBorradorAsync(string plantillaId, CancellationToken ct = default);

    Task ReemplazarAspectosAsync(string plantillaId, IReadOnlyList<AspectoPlantilla> aspectos, CancellationToken ct = default);

    Task PublicarAsync(string plantillaId, CancellationToken ct = default);

    Task DescartarAsync(string plantillaId, CancellationToken ct = default);
}
