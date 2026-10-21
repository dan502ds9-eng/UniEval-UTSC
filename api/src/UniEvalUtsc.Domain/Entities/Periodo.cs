using System;
using UniEvalUtsc.Domain.Enums;
using UniEvalUtsc.Domain.Exceptions;

namespace UniEvalUtsc.Domain.Entities;

public class Periodo
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public DateOnly FechaInicio { get; private set; }
    public DateOnly FechaFin { get; private set; }
    public DateTime EncuestasAbre { get; private set; }
    public DateTime EncuestasCierra { get; private set; }
    public EstadoPeriodo Estado { get; private set; }

    private Periodo() { } // lo usa EF Core al leer de la BD

    public Periodo(string nombre, DateOnly fechaInicio, DateOnly fechaFin,
                   DateTime encuestasAbre, DateTime encuestasCierra)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El periodo necesita nombre.");
        if (fechaInicio > fechaFin)
            throw new DomainException("La fecha de inicio no puede ser después de la de fin.");
        if (encuestasAbre >= encuestasCierra)
            throw new DomainException("La apertura de encuestas debe ser antes del cierre.");

        Nombre = nombre.Trim();
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        EncuestasAbre = encuestasAbre;
        EncuestasCierra = encuestasCierra;
        Estado = EstadoPeriodo.Preparacion;
    }

    // RN-08: solo se contesta con el periodo abierto y dentro de fechas
    public bool AceptaRespuestas(DateTime ahora) =>
        Estado == EstadoPeriodo.Abierto && ahora >= EncuestasAbre && ahora < EncuestasCierra;

    // RN-11: docentes y tutores ven resultados hasta el cierre
    public bool ResultadosVisibles => Estado == EstadoPeriodo.Cerrado;

    public void Abrir()
    {
        if (Estado != EstadoPeriodo.Preparacion)
            throw new DomainException("Solo se puede abrir un periodo en preparación.");
        Estado = EstadoPeriodo.Abierto;
    }

    public void Cerrar()
    {
        if (Estado != EstadoPeriodo.Abierto)
            throw new DomainException("Solo se puede cerrar un periodo abierto.");
        Estado = EstadoPeriodo.Cerrado;
    }
}