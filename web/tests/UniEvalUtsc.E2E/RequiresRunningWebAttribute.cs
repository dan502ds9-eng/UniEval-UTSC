namespace UniEvalUtsc.E2E;

public sealed class RequiresRunningWebAttribute : FactAttribute
{
    public RequiresRunningWebAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UNIEVAL_WEB_URL")))
            Skip = "Configura UNIEVAL_WEB_URL para ejecutar pruebas E2E contra la aplicación web.";
    }
}
