namespace UniEvalUtsc.Web.Services;

public interface IPlantillasService
{
    IReadOnlyList<PlantillaAdministrativa> ObtenerPlantillas();

    PlantillaAdministrativa Obtener(string plantillaId);

    PlantillaAdministrativa CrearBorrador(string plantillaId);

    void ReemplazarAspectos(string plantillaId, IReadOnlyList<AspectoPlantilla> aspectos);

    void Publicar(string plantillaId);

    void Descartar(string plantillaId);
}
